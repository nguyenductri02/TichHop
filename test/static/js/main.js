document.addEventListener('DOMContentLoaded', () => {
  const form = document.getElementById('upload-form');
  const resultDiv = document.getElementById('result');
  const chartsDiv = document.getElementById('charts');
  const btnJson = document.getElementById('download-json');
  const btnExcel = document.getElementById('download-excel');

  let lastData = null;

  form.addEventListener('submit', async function(e) {
    e.preventDefault();
    const fileInput = document.getElementById('source-code');
    if (fileInput.files.length === 0) return;

    const formData = new FormData();
    formData.append('file', fileInput.files[0]);

    resultDiv.innerHTML = '<p>Đang phân tích...</p>';
    chartsDiv.innerHTML = '';
    btnJson.style.display = 'none';
    btnExcel.style.display = 'none';

    try {
      const response = await fetch('/analyze', { method: 'POST', body: formData });
      if (!response.ok) throw new Error('Có lỗi khi phân tích mã nguồn');
      const data = await response.json();
      lastData = data;
      if (data.error) {
        resultDiv.innerHTML = `<p style='color:red;'>${data.error}</p>`;
        return;
      }
      let html = '<h2>Kết quả phân tích:</h2>';
      // Tổng quan project
      if (data.results && data.results.total_files !== undefined) {
        html += `<ul><li><b>Tổng số file:</b> ${data.results.total_files}</li><li><b>Tổng LOC:</b> ${data.results.total_loc}</li><li><b>Độ phức tạp trung bình:</b> ${data.results.average_complexity.toFixed(2)}</li></ul>`;
      }
      // Hiển thị duplication tổng thể nếu có
      let duplicationRendered = false;
      if (data.results && data.results.duplication_metrics) {
        const d = data.results.duplication_metrics;
        html += '<h3>Mức độ Code Duplication:</h3>';
        for (const [lang, metrics] of Object.entries(d)) {
          html += `<b>${lang}</b>: ${metrics.duplication_percentage ? metrics.duplication_percentage.toFixed(2) : '0.00'}%`;
          if (metrics.duplicated_blocks && metrics.duplicated_blocks.length > 0) {
            html += `<details><summary>Có ${metrics.duplicated_blocks.length} đoạn code lặp lại &darr;</summary><ul>`;
            for (const block of metrics.duplicated_blocks) {
              html += `<li><b>${block.file1}</b> (dòng ${block.start1 + 1}) <br/> <b>${block.file2}</b> (dòng ${block.start2 + 1})<br/><pre style="background:#f7f7f9;max-width:95vw;overflow:auto;white-space:pre-wrap;">${block.content.slice(0,500)}${block.content.length>500?'...':''}</pre></li>`;
            }
            html += '</ul></details>';
          }
        }
        duplicationRendered = true;
      }
      // Bảng metrics từng file (chi tiết, đầy đủ)
      if (data.results && data.results.file_metrics) {
        html += '<h3>Chi tiết từng file:</h3>';
        html += '<table border="1" style="border-collapse:collapse;max-width:100%"><tr><th>File</th><th>LOC</th><th>Complexity TB</th><th>Cohesion</th><th>Coupling</th></tr>';
        for (const [file, m] of Object.entries(data.results.file_metrics)) {
          html += `<tr><td>${file.split('/').slice(-2).join('/')}</td>`+
            `<td>${m.loc && m.loc.code_lines !== undefined ? m.loc.code_lines : '-'}</td>`+
            `<td>${m.complexity && m.complexity.average_complexity !== undefined ? m.complexity.average_complexity.toFixed(2) : '-'}</td>`+
            `<td>${m.cohesion_coupling && m.cohesion_coupling.cohesion !== undefined ? m.cohesion_coupling.cohesion.toFixed(2) : '-'}</td>`+
            `<td>${m.cohesion_coupling && m.cohesion_coupling.coupling !== undefined ? m.cohesion_coupling.coupling.toFixed(2) : '-'}</td></tr>`;
        }
        html += '</table>';
      } else if (data.results && data.results.loc) {
        html += `<ul><li><b>LOC:</b> ${data.results.loc.code_lines}</li><li><b>Độ phức tạp:</b> ${data.results.complexity.average_complexity.toFixed(2)}</li></ul>`;
      }
      // Danh sách hàm và độ phức tạp từng hàm per file
      if (data.results && data.results.file_metrics) {
        for (const [file, m] of Object.entries(data.results.file_metrics)) {
          if (m.complexity && Array.isArray(m.complexity.functions) && m.complexity.functions.length > 0) {
            html += `<details><summary>Hàm trong <b>${file.split('/').slice(-2).join('/')}</b> (độ phức tạp):</summary><ul>`;
            for (const f of m.complexity.functions) {
              html += `<li><b>${f.name}</b> (dòng ${f.lineno}): <span style="color:${f.complexity > 10 ? 'red' : f.complexity > 5 ? 'orange' : 'green'}">${f.complexity}</span></li>`;
            }
            html += '</ul></details>';
          }
        }
      }
      // Cohesion & Coupling detail (nếu có)
      if (data.results && data.results.file_metrics) {
        for (const [file, m] of Object.entries(data.results.file_metrics)) {
          if (m.cohesion_coupling && m.cohesion_coupling.details && m.cohesion_coupling.details.classes) {
            html += `<details><summary>Cohesion & Coupling trong <b>${file.split('/').slice(-2).join('/')}</b>:</summary>`;
            html += `<ul><li>Số class: ${m.cohesion_coupling.details.classes}</li><li>Số import: ${m.cohesion_coupling.details.imports}</li></ul>`;
            html += '</details>';
          }
        }
      }
      // Hiển thị các metrics mở rộng khác nếu có
      if (data.results && data.results.metrics) {
        html += '<h3>Metrics mở rộng:</h3><ul>';
        for (const [k, v] of Object.entries(data.results.metrics)) {
          html += `<li><b>${k}:</b> ${typeof v === 'number' ? v.toFixed(2) : v}</li>`;
        }
        html += '</ul>';
      }
      // Gợi ý tối ưu
      let suggestions = [];
      if (data.results && data.results.file_metrics) {
        for (const [file, m] of Object.entries(data.results.file_metrics)) {
          if (Array.isArray(m.suggestions) && m.suggestions.length > 0) {
            suggestions = suggestions.concat(m.suggestions.map(s => ({...s, file: file.split('/').slice(-2).join('/')})));
          }
        }
      }
      if (Array.isArray(data.suggestions) && data.suggestions.length > 0) {
        suggestions = suggestions.concat(data.suggestions);
      }
      if (suggestions.length > 0) {
        html += '<h3>Gợi ý tối ưu:</h3><ul>';
        for (const s of suggestions) {
          html += `<li>[${s.severity ? s.severity.toUpperCase() : 'INFO'}] ${s.file ? '<b>' + s.file + '</b>: ' : ''}${s.message || s}</li>`;
        }
        html += '</ul>';
      }
      resultDiv.innerHTML = html;
      btnJson.style.display = 'inline-block';
      btnExcel.style.display = 'inline-block';
      // Render chart complexity + heatmap
      let chartHtml = '';
      if (data.charts && data.charts.complexity_chart) {
        chartHtml += `<h3>Biểu đồ Cyclomatic Complexity theo module:</h3><img src="${data.charts.complexity_chart}" alt="Cyclomatic Complexity Chart" style="max-width:100%;border:1px solid #bbb;margin-bottom:16px;"/>`;
      }
      if (data.charts && data.charts.cohesion_coupling_chart) {
        chartHtml += `<h3>Heatmap Cohesion & Coupling:</h3><img src="${data.charts.cohesion_coupling_chart}" alt="Heatmap Cohesion Coupling" style="max-width:100%;border:1px solid #bbb;"/>`;
      }
      chartsDiv.innerHTML = chartHtml;
    } catch (err) {
      resultDiv.innerHTML = '<p>Lỗi phân tích - vui lòng thử lại</p>';
      chartsDiv.innerHTML = '';
    }
  });

  // Export JSON
  btnJson.addEventListener('click', function() {
    if (!lastData) return;
    const blob = new Blob([JSON.stringify(lastData, null, 2)], {type: 'application/json'});
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'code-metrics-report.json';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  });

  // Export Excel
  btnExcel.addEventListener('click', function() {
    if (!lastData) return;
    let wb = XLSX.utils.book_new();
    // Dữ liệu từng file
    if (lastData.results && lastData.results.file_metrics) {
      const rows = [['File', 'LOC', 'Complexity avg', 'Cohesion', 'Coupling']];
      for (const [file, m] of Object.entries(lastData.results.file_metrics)) {
        rows.push([
          file.split('/').slice(-2).join('/'),
          m.loc && m.loc.code_lines !== undefined ? m.loc.code_lines : '',
          m.complexity && m.complexity.average_complexity !== undefined ? m.complexity.average_complexity : '',
          m.cohesion_coupling && m.cohesion_coupling.cohesion !== undefined ? m.cohesion_coupling.cohesion : '',
          m.cohesion_coupling && m.cohesion_coupling.coupling !== undefined ? m.cohesion_coupling.coupling : ''
        ]);
      }
      const ws = XLSX.utils.aoa_to_sheet(rows);
      XLSX.utils.book_append_sheet(wb, ws, 'File Metrics');
    }
    // Gợi ý tối ưu
    let suggArr = [];
    if (lastData.results && lastData.results.file_metrics) {
      for (const [file, m] of Object.entries(lastData.results.file_metrics)) {
        if (Array.isArray(m.suggestions) && m.suggestions.length > 0) {
          for (const s of m.suggestions) {
            suggArr.push({File: file.split('/').slice(-2).join('/'), Severity: s.severity || '', Message: s.message || s});
          }
        }
      }
    }
    if (suggArr.length > 0) {
      const ws2 = XLSX.utils.json_to_sheet(suggArr);
      XLSX.utils.book_append_sheet(wb, ws2, 'Suggestions');
    }
    // Duplication
    if (lastData.results && lastData.results.duplication_metrics) {
      let dArr = [];
      const d = lastData.results.duplication_metrics;
      for (const [lang, metrics] of Object.entries(d)) {
        dArr.push({Language: lang, Duplication: metrics.duplication_percentage});
        if (metrics.duplicated_blocks && metrics.duplicated_blocks.length > 0) {
          for (const block of metrics.duplicated_blocks) {
            dArr.push({Block: `${block.file1} (${block.start1 + 1}) ↔ ${block.file2} (${block.start2 + 1})`, Content: block.content.slice(0, 310) });
          }
        }
      }
      if (dArr.length > 0) {
        const ws3 = XLSX.utils.json_to_sheet(dArr);
        XLSX.utils.book_append_sheet(wb, ws3, 'Duplication');
      }
    }
    XLSX.writeFile(wb, 'code-metrics-report.xlsx');
  });
});
