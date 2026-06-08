const fs = require('fs');

const changedFiles = process.argv[2] ? process.argv[2].split(' ').filter(Boolean) : [];
const jsonPath = 'src/Github-Analyzer.WebApi/analyzer_versions.json';
const enumPath = 'src/Github-Analyzer.WebApi/Models/AnalysisType.cs';

if (!fs.existsSync(jsonPath)) 
{
  console.error(`❌ Cannot find ${jsonPath}`);
  process.exit(1);
}

// 1. Dynamically extract analysis types from the C# Enum
let analysisTypes = [];
try {
  if (fs.existsSync(enumPath)) 
  {
    const enumContent = fs.readFileSync(enumPath, 'utf8');
    const match = enumContent.match(/public\s+enum\s+AnalysisType\s*{([^}]+)}/m);
    if (match && match[1]) 
    {
      analysisTypes = match[1].split(',')
        .map(s => s.replace(/\/\/.*|\/\*[\s\S]*?\*\//g, '').trim())
        .filter(s => s.length > 0)
        .map(s => s.split('=')[0].trim());
    }
  }
} catch (e) {
  console.error("❌ Failed to read AnalysisType.cs", e);
}

if (analysisTypes.length === 0) {
  analysisTypes = ['CodeGraph', 'Statistic'];
}

let data = JSON.parse(fs.readFileSync(jsonPath, 'utf8'));
let fail = false;

analysisTypes.forEach(type => 
{
  if (!data[type]) return;
  
  // Ensure CurrentVersion strictly matches the latest History entry
  const history = data[type].History || [];
  if (history.length > 0 && data[type].CurrentVersion !== history[0].Version) 
  {
    console.error(`❌ [${type}] CurrentVersion (${data[type].CurrentVersion}) does not match the latest History entry (${history[0].Version}).`);
    fail = true;
  }
  
  const watchPaths = data[type].WatchPaths || [];
  const matchedCodeChanges = changedFiles.filter(file => 
    watchPaths.some(watch => file.startsWith(watch))
  );
    
  if (matchedCodeChanges.length > 0) 
  {
    const jsonChanged = changedFiles.includes(jsonPath);
    if (!jsonChanged) 
    {
      console.error(`❌ [${type}] Files modified, but analyzer_versions.json not updated.`);
      console.error(`   Modified files:`, matchedCodeChanges);
      fail = true;
    }
  }
});

if (fail) 
{
  console.error('\n👉 Please run: git config core.hooksPath .githooks');
  console.error('   And then commit your changes so the hook updates the JSON automatically.');
  process.exit(1);
}
console.log('✅ Analyzer versions verified.');
