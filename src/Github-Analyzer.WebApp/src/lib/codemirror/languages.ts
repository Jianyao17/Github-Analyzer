import { StreamLanguage, indentUnit } from '@codemirror/language';

export async function loadLanguageExtension(extension: string) 
{
  try 
  {
    switch (extension.toLowerCase()) 
    {
      // Modern Lezer Parsers
      case 'js':
      case 'jsx':
      case 'ts':
      case 'tsx':
        const { javascript } = await import('@codemirror/lang-javascript');
        return [javascript({ jsx: true, typescript: extension.includes('ts') }), indentUnit.of('  ')];

      case 'json':
        const { json } = await import('@codemirror/lang-json');
        return [json(), indentUnit.of('  ')];

      case 'html':
      case 'vue':
        const { html } = await import('@codemirror/lang-html');
        return [html(), indentUnit.of('  ')];

      case 'css':
        const { css } = await import('@codemirror/lang-css');
        return [css(), indentUnit.of('  ')];

      case 'py':
        const { python } = await import('@codemirror/lang-python');
        return [python(), indentUnit.of('    ')];

      case 'cpp':
      case 'c':
      case 'h':
      case 'hpp':
        const { cpp } = await import('@codemirror/lang-cpp');
        return [cpp(), indentUnit.of('    ')];

      case 'java':
        const { java } = await import('@codemirror/lang-java');
        return [java(), indentUnit.of('    ')];

      case 'php':
        const { php } = await import('@codemirror/lang-php');
        return [php(), indentUnit.of('    ')];

      case 'rs':
        const { rust } = await import('@codemirror/lang-rust');
        return [rust(), indentUnit.of('    ')];

      case 'sql':
        const { sql } = await import('@codemirror/lang-sql');
        return [sql(), indentUnit.of('    ')];
      
      case 'cs':
        const { csharp } = await import('@replit/codemirror-lang-csharp');
        return [csharp(), indentUnit.of('    ')];
        
      // Legacy modes
      case 'rb':
        const { ruby } = await import('@codemirror/legacy-modes/mode/ruby');
        return [StreamLanguage.define(ruby), indentUnit.of('  ')];

      case 'go':
        const { go } = await import('@codemirror/legacy-modes/mode/go');
        return [StreamLanguage.define(go), indentUnit.of('\t')];
        
      case 'sh':
      case 'bash':
        const { shell } = await import('@codemirror/legacy-modes/mode/shell');
        return [StreamLanguage.define(shell), indentUnit.of('  ')];
      
      default:
        return null;
    }
  }
  catch (e) 
  {
    console.warn(`Failed to load language for extension ${extension}`, e);
    return null;
  }
}
