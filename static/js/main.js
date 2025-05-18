document.addEventListener('DOMContentLoaded', function() {
    const analyzeForm = document.getElementById('analyzeForm');
    const loading = document.getElementById('loading');
    const results = document.getElementById('results');
    const browseBtn = document.getElementById('browseBtn');
    const projectPath = document.getElementById('projectPath');
    const fileMode = document.getElementById('fileMode');
    const dirMode = document.getElementById('dirMode');
    
    // Xử lý sự kiện click nút Browse
    $('#analyzeButton').click(function() {
        const fileInput = $('#fileInput')[0];
        const file = fileInput.files[0];
        const mode = $('input[name="analysisType"]:checked').val();

        if (!file) {
            alert('Vui lòng chọn file.');
            return;
        }

        $('#results').html('<div class="text-center"><div class="spinner-border" role="status"></div><p>Đang phân tích...</p></div>');

        const formData = new FormData();
        formData.append('file', file);
        formData.append('analysis_mode', mode);

        $.ajax({
            url: '/analyze',
            method: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function(response) {
                if (response.error) {
                    $('#results').html(`<div class="alert alert-danger">${response.error}</div>`);
                } else {
                    displayResults(response);
                }
            },
            error: function(xhr, status, error) {
                $('#results').html(`<div class="alert alert-danger">Có lỗi khi phân tích: ${error}</div>`);
            }
        });
    });

    // Xử lý submit form
    analyzeForm.addEventListener('submit', function(e) {
        e.preventDefault();
        
        if (!projectPath.value) {
            alert('Vui lòng chọn đường dẫn để phân tích');
            return;
        }
        
        // Hiển thị loading
        loading.style.display = 'block';
        results.style.display = 'none';
        
        // Gửi request phân tích
        fetch('/analyze', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
            },
            body: 'project_path=' + encodeURIComponent(projectPath.value) + 
                  '&analysis_mode=' + (fileMode.checked ? 'file' : 'directory')
        })
        .then(response => response.json())
        .then(data => {
            // Kiểm tra lỗi
            if (data.error) {
                alert('Lỗi: ' + data.error);
                loading.style.display = 'none';
                return;
            }
            
            // Hiển thị kết quả
            displayResults(data);
            
            // Ẩn loading, hiện kết quả
            loading.style.display = 'none';
            results.style.display = 'block';
        })
        .catch(error => {
            console.error('Lỗi:', error);
            alert('Có lỗi xảy ra khi phân tích');
            loading.style.display = 'none';
        });
    });
    
    function displayResults(data) {
        const results = data.results;
        const projectMetrics = results.project_metrics;
        const fileMetrics = results.file_metrics;
        
        // Hiển thị thông tin tổng quan
        let overviewHTML = `
            <div class="card">
                <h2>Tổng quan</h2>
                <table>
                    <tr>
                        <td>Số lượng file:</td>
                        <td>${projectMetrics.total_files || 0}</td>
                    </tr>
                    <tr>
                        <td>Tổng số dòng code:</td>
                        <td>${projectMetrics.total_loc || 0}</td>
                    </tr>
                    <tr>
                        <td>Độ phức tạp trung bình:</td>
                        <td>${projectMetrics.average_complexity !== undefined ? projectMetrics.average_complexity.toFixed(2) : 'N/A'}</td>
                    </tr>
                    <tr>
                        <td>Cohesion trung bình:</td>
                        <td>${projectMetrics.average_cohesion !== undefined ? projectMetrics.average_cohesion.toFixed(2) : 'N/A'}</td>
                    </tr>
                    <tr>
                        <td>Coupling trung bình:</td>
                        <td>${projectMetrics.average_coupling !== undefined ? projectMetrics.average_coupling.toFixed(2) : 'N/A'}</td>
                    </tr>
                    <tr>
                        <td>Trùng lặp code trung bình:</td>
                        <td>${projectMetrics.average_duplication !== undefined ? projectMetrics.average_duplication.toFixed(2) + '%' : 'N/A'}</td>
                    </tr>
                    <tr>
                        <td>Thời gian phân tích:</td>
                        <td>${results.execution_time !== undefined ? results.execution_time.toFixed(2) + ' giây' : 'N/A'}</td>
                    </tr>
                </table>
            </div>
        `;
        document.getElementById('overview').innerHTML = overviewHTML;
        
        // Hiển thị biểu đồ
        let chartsHTML = '<div class="card"><h2>Biểu đồ phân tích</h2>';
        if (data.charts && data.charts.complexity_chart) {
            chartsHTML += `
                <div class="chart">
                    <h3>Biểu đồ độ phức tạp</h3>
                    <img src="${data.charts.complexity_chart}" alt="Complexity Chart">
                </div>
            `;
        }
        
        if (data.charts && data.charts.cohesion_coupling_chart) {
            chartsHTML += `
                <div class="chart">
                    <h3>Biểu đồ Cohesion & Coupling</h3>
                    <img src="${data.charts.cohesion_coupling_chart}" alt="Cohesion & Coupling Chart">
                </div>
            `;
        }
        chartsHTML += '</div>';
        document.getElementById('charts').innerHTML = chartsHTML;
        
        // Hiển thị metrics cho từng file
        let fileMetricsHTML = `
            <div class="card">
                <h2>Chi tiết từng file</h2>
                <table class="metrics-table">
                    <thead>
                        <tr>
                            <th>File</th>
                            <th>LOC</th>
                            <th>Độ phức tạp</th>
                            <th>Cohesion</th>
                            <th>Coupling</th>
                        </tr>
                    </thead>
                    <tbody>
        `;
        
        for (const [filePath, metrics] of Object.entries(fileMetrics)) {
            const fileName = filePath.split('/').pop(); // Lấy tên file từ đường dẫn
            fileMetricsHTML += `
                <tr>
                    <td>${fileName}</td>
                    <td>${metrics.loc && metrics.loc.code_lines !== undefined ? metrics.loc.code_lines : 'N/A'}</td>
                    <td>${metrics.complexity && metrics.complexity.average_complexity !== undefined ? metrics.complexity.average_complexity.toFixed(2) : 'N/A'}</td>
                    <td>${metrics.cohesion_coupling && metrics.cohesion_coupling.cohesion !== undefined ? metrics.cohesion_coupling.cohesion.toFixed(2) : 'N/A'}</td>
                    <td>${metrics.cohesion_coupling && metrics.cohesion_coupling.coupling !== undefined ? metrics.cohesion_coupling.coupling.toFixed(2) : 'N/A'}</td>
                </tr>
            `;
        }
        
        fileMetricsHTML += `
                    </tbody>
                </table>
            </div>
        `;
        document.getElementById('fileMetrics').innerHTML = fileMetricsHTML;
        
        // Hiển thị gợi ý cải thiện
        let suggestionsHTML = `
            <div class="card">
                <h2>Gợi ý cải thiện code</h2>
        `;
        
        let hasSuggestions = false;
        for (const [filePath, metrics] of Object.entries(fileMetrics)) {
            if (metrics.suggestions && metrics.suggestions.length > 0) {
                hasSuggestions = true;
                const fileName = filePath.split('/').pop();
                
                suggestionsHTML += `<div class="file-suggestions"><h3>${fileName}</h3><ul>`;
                
                for (const suggestion of metrics.suggestions) {
                    let severityClass = '';
                    let severityIcon = '';
                    
                    if (suggestion.severity === 'high') {
                        severityClass = 'high-severity';
                        severityIcon = '<i class="fas fa-exclamation-circle text-red-600 mr-2"></i>';
                    } else if (suggestion.severity === 'medium') {
                        severityClass = 'medium-severity';
                        severityIcon = '<i class="fas fa-exclamation-triangle text-yellow-600 mr-2"></i>';
                    } else {
                        severityClass = 'low-severity';
                        severityIcon = '<i class="fas fa-info-circle text-blue-600 mr-2"></i>';
                    }
                    
                    suggestionsHTML += `<li class="${severityClass}">${severityIcon}${suggestion.message}`;
                    
                    // Add code location if available
                    if (suggestion.location) {
                        suggestionsHTML += ` <span class="text-gray-500">(Line: ${suggestion.location.line})</span>`;
                    }
                    
                    // Add fix suggestion if available
                    if (suggestion.fixSuggestion) {
                        suggestionsHTML += `<div class="mt-1 ml-6 p-2 bg-gray-100 rounded text-sm font-mono">${suggestion.fixSuggestion}</div>`;
                    }
                    
                    suggestionsHTML += `</li>`;
                }
                
                suggestionsHTML += `</ul></div>`;
            }
        }
        
        if (!hasSuggestions) {
            suggestionsHTML += '<p>Không có gợi ý cải thiện nào.</p>';
        }
        
        suggestionsHTML += '</div>';
        document.getElementById('suggestions').innerHTML = suggestionsHTML;
    }
});