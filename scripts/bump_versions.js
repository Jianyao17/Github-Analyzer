const fs = require('fs');
const crypto = require('crypto');
const { execSync } = require('child_process');

let stagedFilesOutput = "";
try 
{
  // Get all staged files
  stagedFilesOutput = execSync(
    'git diff --cached --name-only', 
    { 
      encoding: 'utf8', 
      stdio: ['ignore', 'pipe', 'ignore'] 
    })
    .trim();
}
catch(e) 
{
  process.exit(0);
}

if (!stagedFilesOutput) process.exit(0);

const stagedFiles = stagedFilesOutput.split('\n').map(f => f.trim()).filter(f => f);

const jsonPath = 'src/Github-Analyzer.WebApi/analyzer_versions.json';
const enumPath = 'src/Github-Analyzer.WebApi/Models/AnalysisType.cs';

if (!fs.existsSync(jsonPath)) {
  process.exit(0);
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
      // Split by comma, remove comments and whitespace
      analysisTypes = match[1].split(',')
        .map(s => s.replace(/\/\/.*|\/\*[\s\S]*?\*\//g, '').trim())
        .filter(s => s.length > 0)
        .map(s => s.split('=')[0].trim()); // handle explicit values if any
    }
  }
} catch (e) {
  console.error("[Pre-commit] Failed to read AnalysisType.cs", e);
}

// Fallback just in case
if (analysisTypes.length === 0) {
  analysisTypes = ['CodeGraph', 'Statistic'];
}

let data;
try 
{
  data = JSON.parse(fs.readFileSync(jsonPath, 'utf8'));
} 
catch (e) 
{
  console.error("[Pre-commit] Failed to parse analyzer_versions.json", e);
  process.exit(0);
}

let changed = false;

function generateVersion() 
{
  const d = new Date(); // Format YYYYMMDD_HHMMSS
  const ts = d.toISOString().replace(/[-:T]/g, '').slice(0, 14); 
  const hash = crypto.randomBytes(4).toString('hex').slice(0, 7);
    
  return `${ts.slice(0,8)}_${ts.slice(8,14)}-${hash}`;
}

analysisTypes.forEach(type => 
{
  // If the JSON doesn't track this type yet, initialize it
  if (!data[type]) 
  {
    const initVersion = generateVersion();
    data[type] = { 
      CurrentVersion: initVersion, 
      WatchPaths: [], 
      History: [{
        Version: initVersion,
        Timestamp: new Date().toISOString(),
        ChangedFiles: []
      }] 
    };
    changed = true;
  }
  
  const watchPaths = data[type].WatchPaths || [];
  const matchedFiles = stagedFiles.filter(file => 
    watchPaths.some(watch => file.startsWith(watch))
  );

  if (matchedFiles.length > 0) 
  {
    const newVersion = generateVersion();
    data[type].CurrentVersion = newVersion;
      
    if (!data[type].History) 
      data[type].History = [];
      
    data[type].History.unshift({
      Version: newVersion,
      Timestamp: new Date().toISOString(),
      ChangedFiles: matchedFiles
    });
    
    changed = true;
  }
});

if (changed) 
{
  fs.writeFileSync(jsonPath, JSON.stringify(data, null, 2) + '\n');
  console.log("[Pre-commit] Analyzer versions bumped! Adding analyzer_versions.json to commit.");
  try {
      execSync(`git add ${jsonPath}`);
  } catch(e) {}
}
