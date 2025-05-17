from flask import Flask, request, jsonify
from radon.complexity import cc_visit
from radon.metrics import mi_visit, h_visit
from radon.raw import analyze
import lizard
import os
import tempfile
import shutil
import zipfile

app = Flask(__name__, static_folder='static', static_url_path='/static')

def analyze_python_code(code):
    raw_metrics = analyze(code)
    loc = raw_metrics.loc
    lloc = raw_metrics.lloc
    sloc = raw_metrics.sloc
    comments = raw_metrics.comments
    multi = raw_metrics.multi
    blank = raw_metrics.blank

    complexity = cc_visit(code)
    avg_cyclomatic = sum([c.complexity for c in complexity])/len(complexity) if complexity else 0

    cohesion = 0.8  # Placeholder
    coupling = 0.7  # Placeholder

    return {
        "loc": loc,
        "lloc": lloc,
        "sloc": sloc,
        "comments": comments,
        "multi": multi,
        "blank": blank,
        "avg_cyclomatic_complexity": round(avg_cyclomatic, 2),
        "cohesion": cohesion,
        "coupling": coupling
    }

def analyze_single_file(file_path):
    """Analyze a single file and return its metrics"""
    if file_path.endswith('.py'):
        with open(file_path, 'r', encoding='utf-8') as f:
            code = f.read()
        metrics = analyze_python_code(code)
        metrics['code_duplication_percent'] = 0  # No duplication with a single file
    else:  # Assuming Java file
        analysis = lizard.analyze_file(file_path)
        metrics = process_lizard_file_metrics(analysis)
    
    return metrics

def process_lizard_file_metrics(file_info):
    """Process metrics for a single file from lizard analysis"""
    avg_cyclomatic = 0
    functions_count = len(file_info.function_list)
    
    for func in file_info.function_list:
        avg_cyclomatic += func.cyclomatic_complexity
        
    avg_cyclomatic = avg_cyclomatic / functions_count if functions_count > 0 else 0
    
    return {
        "loc": file_info.nloc,
        "avg_cyclomatic_complexity": round(avg_cyclomatic, 2),
        "code_duplication_percent": 0,  # Simplified, lizard doesn't directly provide this
        "cohesion": 0.75,  # Placeholder
        "coupling": 0.65   # Placeholder
    }

@app.route('/api/analyze', methods=['POST'])
def analyze_code():
    if 'codefile' not in request.files and 'zipfile' not in request.files:
        return jsonify({"error": "No file uploaded"}), 400

    temp_dir = tempfile.mkdtemp()
    try:
        if 'zipfile' in request.files:
            zip_file = request.files['zipfile']
            if not zip_file.filename.endswith('.zip'):
                return jsonify({"error": "Only ZIP files are supported for multiple files"}), 400
            
            zip_path = os.path.join(temp_dir, 'upload.zip')
            zip_file.save(zip_path)
            
            with zipfile.ZipFile(zip_path, 'r') as zip_ref:
                zip_ref.extractall(temp_dir)
                
            source_files = []
            for root, dirs, files in os.walk(temp_dir):
                for file in files:
                    if file.endswith('.py') or file.endswith('.java'):
                        source_files.append(os.path.join(root, file))
                        
            if not source_files:
                return jsonify({"error": "No Python or Java files found in the ZIP"}), 400
            
            # Analyze each file individually
            file_metrics = []
            for file_path in source_files:
                rel_path = os.path.relpath(file_path, temp_dir)
                metrics = analyze_single_file(file_path)
                metrics['filename'] = rel_path
                
                # Add suggestions for this individual file
                suggestions = []
                if metrics['avg_cyclomatic_complexity'] > 10:
                    suggestions.append(f"Consider refactoring functions in {rel_path} to reduce cyclomatic complexity.")
                if metrics.get('code_duplication_percent', 0) > 10:
                    suggestions.append(f"Reduce duplicated code in {rel_path} to improve maintainability.")
                if metrics.get('cohesion', 1) < 0.6:
                    suggestions.append(f"Improve module cohesion in {rel_path} for better code quality.")
                if metrics.get('coupling', 0) > 0.8:
                    suggestions.append(f"Reduce coupling between modules in {rel_path}.")
                
                metrics['suggestions'] = suggestions
                file_metrics.append(metrics)
            
            # Return results for all files
            result = {
                "is_zip": True,
                "file_metrics": file_metrics,
                "analyzed_files": [os.path.relpath(f, temp_dir) for f in source_files]
            }
            
        else:  # Single file upload
            code_file = request.files['codefile']
            code = code_file.read().decode('utf-8')
            filename = code_file.filename
            
            if filename.endswith('.py'):
                metrics = analyze_python_code(code)
                metrics['analyzed_files'] = [filename]
                metrics['code_duplication_percent'] = 0  # No duplication with 1 file
                
            elif filename.endswith('.java'):
                file_path = os.path.join(temp_dir, filename)
                with open(file_path, 'w', encoding='utf-8') as f:
                    f.write(code)
                metrics = analyze_lizard([file_path])
                metrics['analyzed_files'] = [filename]
                
            else:
                return jsonify({"error": "Unsupported file type"}), 400

            # Add suggestions for single file
            suggestions = []
            if metrics['avg_cyclomatic_complexity'] > 10:
                suggestions.append("Consider refactoring functions to reduce cyclomatic complexity.")
            if metrics.get('code_duplication_percent', 0) > 10:
                suggestions.append("Reduce duplicated code to improve maintainability.")
            if metrics.get('cohesion', 1) < 0.6:
                suggestions.append("Improve module cohesion for better code quality.")
            if metrics.get('coupling', 0) > 0.8:
                suggestions.append("Reduce coupling between modules.")
                
            metrics['suggestions'] = suggestions
            result = metrics
            result['is_zip'] = False
            
        return jsonify(result)
        
    finally:
        shutil.rmtree(temp_dir)

def analyze_lizard(files):
    """Analyze multiple files using lizard - for backward compatibility"""
    analysis = lizard.analyze(files)
    avg_cyclomatic = 0
    functions_count = 0
    total_lines = 0
    
    for file_info in analysis:
        total_lines += file_info.nloc
        for func in file_info.function_list:
            avg_cyclomatic += func.cyclomatic_complexity
            functions_count += 1
            
    # Handle case with no functions
    avg_cyclomatic = avg_cyclomatic / functions_count if functions_count > 0 else 0
    
    # Calculate code duplication rate (simplified as lizard doesn't provide this directly)
    duplication_percent = 0
    
    cohesion = 0.75
    coupling = 0.65

    return {
        "loc": total_lines,
        "avg_cyclomatic_complexity": round(avg_cyclomatic, 2),
        "code_duplication_percent": round(duplication_percent, 2),
        "cohesion": cohesion,
        "coupling": coupling
    }

@app.route('/')
def index():
    return """
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8" />
<meta name="viewport" content="width=device-width, initial-scale=1" />
<title>Code Quality Metrics Tool</title>
<script crossorigin src="https://unpkg.com/react@18/umd/react.development.js"></script>
<script crossorigin src="https://unpkg.com/react-dom@18/umd/react-dom.development.js"></script>
<script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
<style>
  @import url('https://fonts.googleapis.com/css2?family=Montserrat:wght@400;700&display=swap');

  body {
    font-family: 'Montserrat', sans-serif;
    margin: 0; padding: 0; background: #121212; color: #e0e0e0;
    display: flex; flex-direction: column; min-height: 100vh;
  }
  header {
    background: linear-gradient(90deg, #0f2027, #203a43, #2c5364);
    padding: 1.5rem; text-align: center; font-size: 2rem;
    font-weight: 700; letter-spacing: 3px; color: #a5d8ff;
    box-shadow: 0 4px 10px rgba(0,0,0,0.5);
  }
  main {
    flex: 1; padding: 2rem; max-width: 960px; margin: auto;
    background: #1e1e2e; border-radius: 12px;
    box-shadow: 0 8px 20px rgba(0,0,0,0.8);
  }
  label {
    display: block; margin-bottom: 0.5rem; font-weight: 600;
    color: #a0c4ff;
  }
  input[type="file"] {
    margin-bottom: 1rem;
    border-radius: 6px;
    border: 1px solid #555;
    padding: 0.4rem;
    background: #2a2a3c;
    color: #ddd;
  }
  input[type="file"]:focus {
    outline: 2px solid #3da9fc;
  }
  button {
    background: #3da9fc; color: white; border: none;
    padding: 0.75rem 1.5rem; font-size: 1.1rem;
    font-weight: 700; border-radius: 8px;
    cursor: pointer; box-shadow: 0 4px 12px rgba(61,169,252,0.6);
    transition: background 0.3s ease, box-shadow 0.3s ease;
    margin-top: 0.5rem;
  }
  button:hover {
    background: #1a82f7;
    box-shadow: 0 6px 20px rgba(26,130,247,0.9);
  }
  button:disabled {
    background: #555;
    cursor: not-allowed;
    box-shadow: none;
  }
  .result {
    margin-top: 2rem;
  }
  .metrics-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit,minmax(220px,1fr));
    gap: 1.5rem;
    margin-top: 1rem;
  }
  .metric-card {
    background: #292941;
    padding: 1.25rem 1.5rem;
    border-radius: 14px;
    box-shadow: 0 6px 14px rgba(0,0,0,0.7);
    transition: transform 0.3s ease, box-shadow 0.3s ease;
    cursor: default;
  }
  .metric-card:hover {
    transform: translateY(-6px);
    box-shadow: 0 14px 32px rgba(61,169,252,0.6);
  }
  .metric-title {
    font-weight: 700;
    font-size: 1.2rem;
    color: #72c7ff;
    margin-bottom: 0.3rem;
  }
  .metric-value {
    font-size: 2rem;
    font-weight: 700;
    color: #f0f0f0;
    margin-bottom: 0.25rem;
  }
  .metric-desc {
    color: #a0a9b8;
    font-size: 0.9rem;
    line-height: 1.2rem;
  }
  .suggestions {
    margin-top: 2rem;
    background: #0c2147;
    padding: 1.5rem;
    border-radius: 14px;
    color: #ffcc80;
    box-shadow: 0 6px 20px rgba(255,204,128,0.5);
  }
  .suggestions h3 {
    margin-top: 0;
    margin-bottom: 1rem;
    font-weight: 700;
    font-size: 1.4rem;
    letter-spacing: 1px;
    color: #ffc870;
  }
  .suggestions ul {
    margin: 0;
    padding-left: 1.5rem;
  }
  .suggestions li {
    padding: 0.3rem 0;
    font-weight: 600;
  }
  canvas {
    max-width: 100%;
    margin-top: 1rem;
    background: #252536;
    border-radius: 12px;
    box-shadow: 0 6px 16px rgba(0,0,0,0.6);
  }
  .file-info {
    margin-top: 0.5rem;
    font-style: italic;
    color: #9bbbd4;
  }
  .error-message {
    margin-top: 1rem;
    background: #bf3d3d;
    padding: 0.7rem 1rem;
    border-radius: 8px;
    color: white;
    font-weight: 700;
    box-shadow: 0 4px 12px rgba(191,61,61,0.7);
  }
  .file-tabs {
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
    margin: 1.5rem 0;
  }
  .file-tab {
    background: #2a2a42;
    padding: 0.5rem 1rem;
    border-radius: 8px;
    cursor: pointer;
    transition: all 0.2s ease;
    font-weight: 600;
    border: none;
    color: #ccc;
  }
  .file-tab:hover {
    background: #3a3a62;
  }
  .file-tab.active {
    background: #3da9fc;
    color: white;
  }
  .file-metrics {
    border: 1px solid #3a3a5a;
    border-radius: 12px;
    padding: 1.5rem;
    margin-top: 1rem;
    background: rgba(42, 42, 66, 0.5);
  }
  .file-heading {
    font-size: 1.3rem;
    color: #a0c4ff;
    margin-bottom: 1rem;
    border-bottom: 1px solid #4a4a7a;
    padding-bottom: 0.5rem;
  }
</style>
</head>
<body>
<header>Code Quality Metrics Tool</header>
<main>
  <div id="app"></div>
</main>
<script type="text/javascript" crossorigin="anonymous" src="https://unpkg.com/react@18/umd/react.development.js"></script>
<script type="text/javascript" crossorigin="anonymous" src="https://unpkg.com/react-dom@18/umd/react-dom.development.js"></script>
<script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
<script type="text/javascript">

const e = React.createElement;

function App() {
  const [file, setFile] = React.useState(null);
  const [zipFile, setZipFile] = React.useState(null);
  const [loading, setLoading] = React.useState(false);
  const [metrics, setMetrics] = React.useState(null);
  const [error, setError] = React.useState(null);
  const [activeFileIndex, setActiveFileIndex] = React.useState(0);
  const chartRef1 = React.useRef(null);
  const chartRef2 = React.useRef(null);

  const onFileChange = (event) => {
    setFile(event.target.files[0]);
    setZipFile(null);
    setMetrics(null);
    setError(null);
  };

  const onZipFileChange = (event) => {
    setZipFile(event.target.files[0]);
    setFile(null);
    setMetrics(null);
    setError(null);
  }

  const submit = () => {
    setLoading(true);
    setError(null);
    setMetrics(null);
    const formData = new FormData();
    if (zipFile) {
      formData.append('zipfile', zipFile);
    } else if (file) {
      formData.append('codefile', file);
    } else {
      setError("Please select a file or ZIP to upload.");
      setLoading(false);
      return;
    }
    fetch('/api/analyze', {
      method: 'POST',
      body: formData
    }).then(res => res.json())
      .then(data => {
        if(data.error) {
          setError(data.error);
          setMetrics(null);
        } else {
          setMetrics(data);
          setActiveFileIndex(0);
          setTimeout(() => {
            drawCharts(data);
          }, 100);
        }
      })
      .catch(() => {
        setError("Failed to analyze code, try again.");
        setMetrics(null);
      })
      .finally(() => {
        setLoading(false);
      });
  };

  function drawCharts(data) {
    if(chartRef1.current) {
      chartRef1.current.destroy();
    }
    if(chartRef2.current) {
      chartRef2.current.destroy();
    }

    // Get the right metrics depending on whether it's a zip file or single file
    let metricsToShow;
    if (data.is_zip) {
      metricsToShow = data.file_metrics[activeFileIndex];
    } else {
      metricsToShow = data;
    }

    const ctx1 = document.getElementById('chart1');
    if (ctx1) {
      const context = ctx1.getContext('2d');
      chartRef1.current = new Chart(context, {
        type: 'bar',
        data: {
          labels: ['Lines of Code (LOC)', 'Avg Cyclomatic Complexity'],
          datasets: [{
            label: 'Metrics',
            data: [metricsToShow.loc, metricsToShow.avg_cyclomatic_complexity],
            backgroundColor: ['#42a5f5', '#66bb6a']
          }]
        },
        options: {
          responsive: true,
          scales: {
            y: {
              beginAtZero: true,
              ticks: { stepSize: 1 }
            }
          },
          plugins: {
            legend: { display: false }
          }
        }
      });
    }

    const ctx2 = document.getElementById('chart2');
    if (ctx2) {
      const context = ctx2.getContext('2d');
      chartRef2.current = new Chart(context, {
        type: 'doughnut',
        data: {
          labels: ['Cohesion (%)', 'Coupling (%)'],
          datasets: [{
            label: 'Quality Metrics',
            data: [metricsToShow.cohesion * 100, metricsToShow.coupling * 100],
            backgroundColor: ['#ffa726', '#ef5350']
          }]
        },
        options: {
          responsive: true,
          cutout: '70%',
          plugins: {
            legend: {
              labels: {
                color: '#e0e0e0',
                font: {size: 14, weight: '700'}
              }
            }
          }
        }
      });
    }
  }

  // Function to switch between files in a ZIP
  const showFileMetrics = (index) => {
    setActiveFileIndex(index);
    setTimeout(() => {
      if (metrics && metrics.is_zip) {
        drawCharts(metrics);
      }
    }, 50);
  };

  // Render metrics for a single file (used for both single file upload and for showing ZIP file content)
  const renderFileMetrics = (fileMetrics, filename) => {
    return e('div', {className: 'file-metrics'},
      filename && e('h3', {className: 'file-heading'}, filename),
      e('div', {className: 'metrics-grid'},
        e('div', {className: 'metric-card'},
          e('div', {className: 'metric-title'}, 'Lines of Code (LOC)'),
          e('div', {className: 'metric-value'}, fileMetrics.loc),
          e('div', {className: 'metric-desc'}, 'Số dòng lệnh trong mã nguồn.')
        ),
        e('div', {className: 'metric-card'},
          e('div', {className: 'metric-title'}, 'Cyclomatic Complexity'),
          e('div', {className: 'metric-value'}, fileMetrics.avg_cyclomatic_complexity),
          e('div', {className: 'metric-desc'}, 'Đo lường độ phức tạp của luồng điều khiển.')
        ),
        (fileMetrics.code_duplication_percent !== null && e('div', {className: 'metric-card'},
          e('div', {className: 'metric-title'}, 'Code Duplication (%)'),
          e('div', {className: 'metric-value'}, fileMetrics.code_duplication_percent),
          e('div', {className: 'metric-desc'}, 'Kiểm tra code lặp lại.')
        )),
        e('div', {className: 'metric-card'},
          e('div', {className: 'metric-title'}, 'Cohesion & Coupling'),
          e('div', {className: 'metric-value'}, 
            e('span', null, `Cohesion: ${Math.round(fileMetrics.cohesion * 100)}% `),
            e('br'),
            e('span', null, `Coupling: ${Math.round(fileMetrics.coupling * 100)}%`)
          ),
          e('div', {className: 'metric-desc'}, 'Đo mức độ gắn kết và kết nối giữa các thành phần trong module.')
        )
      ),

      e('canvas', {id: 'chart1', width: 400, height: 200}),
      e('canvas', {id: 'chart2', width: 400, height: 200}),

      fileMetrics.suggestions && fileMetrics.suggestions.length > 0 && e('div', {className: 'suggestions'},
        e('h3', null, 'Suggestions for Code Improvement'),
        e('ul', null, fileMetrics.suggestions.map((item, idx) => e('li', {key: idx}, item)))
      )
    );
  };

  return e('div', null,
    e('div', null,
      e('label', {htmlFor: 'fileInput'}, 'Upload a Python or Java source file:'),
      e('input', {
        type: 'file',
        accept: '.py,.java',
        onChange: onFileChange,
        disabled: loading || zipFile !== null,
        id: 'fileInput'
      }),
    ),
    e('div', null,
      e('label', {htmlFor: 'zipInput'}, 'Or upload a ZIP with multiple source files:'),
      e('input', {
        type: 'file',
        accept: '.zip',
        onChange: onZipFileChange,
        disabled: loading || file !== null,
        id: 'zipInput'
      }),
    ),
    e('button', {onClick: submit, disabled: loading}, loading ? 'Analyzing...' : 'Analyze Code'),

    error && e('div', {className: 'error-message'}, error),

    metrics && e('div', {className: 'result'},
      e('h2', {style: {color: '#72c7ff', marginBottom: '0.5rem'}}, 'Analysis Result'),
      
      // Display file selector tabs for ZIP files
      metrics.is_zip && e('div', null,
        e('div', {className: 'file-info'}, `Found ${metrics.analyzed_files.length} source files in ZIP archive`),
        e('div', {className: 'file-tabs'}, 
          metrics.file_metrics.map((file, index) => 
            e('button', {
              className: `file-tab ${activeFileIndex === index ? 'active' : ''}`,
              onClick: () => showFileMetrics(index),
              key: index
            }, file.filename)
          )
        ),
        // Show metrics for the active file only
        metrics.file_metrics.length > 0 && renderFileMetrics(metrics.file_metrics[activeFileIndex])
      ),
      
      // Display metrics for single file upload
      !metrics.is_zip && renderFileMetrics(metrics)
    )
  );
}

ReactDOM.createRoot(document.getElementById('app')).render(e(App));
</script>
</body>
</html>
"""

if __name__ == '__main__':
    app.run(debug=True)