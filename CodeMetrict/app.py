import os
import re
import ast
import javalang
import difflib
import pandas as pd
import numpy as np
import networkx as nx
from flask import Flask, render_template, request, jsonify, send_file
from collections import defaultdict
import matplotlib.pyplot as plt
import seaborn as sns
import base64
from io import BytesIO
import time
import lizard
from radon.complexity import cc_visit
from radon.metrics import h_visit
from radon.raw import analyze
from tkinter import Tk, filedialog
import tkinter as tk

app = Flask(__name__, static_folder='static')

class CodeMetricsAnalyzer:
    def __init__(self):
        self.supported_languages = ['python', 'java']
        self.results = {}
        
    def detect_language(self, file_path):
        """Phát hiện ngôn ngữ lập trình từ phần mở rộng của file"""
        try:
            extension = os.path.splitext(file_path)[1].lower()
            print(f"Detected file extension: {extension}")  # Debug log
            
            if extension == '.py':
                return 'python'
            elif extension == '.java':
                return 'java'
            else:
                print(f"Unsupported file extension: {extension}")  # Debug log
                return None
        except Exception as e:
            print(f"Error detecting language: {str(e)}")  # Debug log
            return None
    
    def load_file(self, file_path):
        """Đọc nội dung file"""
        try:
            # Chuyển đổi đường dẫn về dạng chuẩn của hệ điều hành
            file_path = os.path.abspath(os.path.normpath(file_path))
            print(f"Loading file from: {file_path}")  # Debug log
            
            with open(file_path, 'r', encoding='utf-8') as file:
                content = file.read()
                print(f"Successfully read {len(content)} characters")  # Debug log
                return content
        except Exception as e:
            print(f"Error reading file {file_path}: {str(e)}")  # Debug log
            return None
    
    def measure_loc(self, code, language):
        """Đo Lines of Code (LOC)"""
        if not code:
            return {
                'total_lines': 0,
                'code_lines': 0,
                'comment_lines': 0,
                'blank_lines': 0
            }
            
        lines = code.split('\n')
        total_lines = len(lines)
        blank_lines = sum(1 for line in lines if not line.strip())
        
        if language == 'python':
            # Phân tích code Python
            analysis = analyze(code)
            return {
                'total_lines': total_lines,
                'code_lines': analysis.loc,
                'comment_lines': analysis.comments,
                'blank_lines': blank_lines
            }
        elif language == 'java':
            # Đếm số dòng comment và code cho Java
            comment_lines = 0
            in_block_comment = False
            
            for i, line in enumerate(lines):
                line = line.strip()
                if not line:
                    continue
                
                if in_block_comment:
                    comment_lines += 1
                    if '*/' in line:
                        in_block_comment = False
                elif line.startswith('//'):
                    comment_lines += 1
                elif '/*' in line:
                    comment_lines += 1
                    if '*/' not in line[line.find('/*')+2:]:
                        in_block_comment = True
                
            return {
                'total_lines': total_lines,
                'code_lines': total_lines - blank_lines - comment_lines,
                'comment_lines': comment_lines,
                'blank_lines': blank_lines
            }
        
    def measure_cyclomatic_complexity(self, code, language, file_path):
        """Đo độ phức tạp Cyclomatic Complexity"""
        if not code:
            return {
                'average_complexity': 0,
                'functions': []
            }

        if language == 'python':
            # Sử dụng radon để tính toán cho Python
            functions = []
            try:
                metrics = cc_visit(code)
                
                if not metrics:
                    return {
                        'average_complexity': 0,
                        'functions': []
                    }
                
                total_complexity = 0
                for func in metrics:
                    functions.append({
                        'name': func.name,
                        'complexity': func.complexity,
                        'lineno': func.lineno
                    })
                    total_complexity += func.complexity
                
                average = total_complexity / len(metrics) if metrics else 0
                
                return {
                    'average_complexity': average,
                    'functions': functions
                }
            except Exception as e:
                print(f"Lỗi khi phân tích CC cho Python: {e}")
                return {
                    'average_complexity': 0,
                    'functions': []
                }
                
        elif language == 'java':
            # Sử dụng lizard để tính toán cho Java
            try:
                analysis = lizard.analyze_file.analyze_source_code(file_path, code)
                functions = []
                total_complexity = 0
                
                for func in analysis.function_list:
                    functions.append({
                        'name': func.name,
                        'complexity': func.cyclomatic_complexity,
                        'lineno': func.start_line
                    })
                    total_complexity += func.cyclomatic_complexity
                
                average = total_complexity / len(analysis.function_list) if analysis.function_list else 0
                
                return {
                    'average_complexity': average,
                    'functions': functions
                }
            except Exception as e:
                print(f"Lỗi khi phân tích CC cho Java: {e}")
                return {
                    'average_complexity': 0,
                    'functions': []
                }
    
    def measure_duplication(self, files_content, language):
        """Đo lường mức độ Code Duplication"""
        if not files_content:
            return {
                'duplication_percentage': 0,
                'duplicated_blocks': []
            }

        # Chuẩn bị tất cả nội dung code để phân tích
        all_code = []
        for file_path, content in files_content.items():
            lines = content.split('\n')
            processed_lines = []
            
            # Bỏ qua comment và dòng trống
            if language == 'python':
                in_multiline_comment = False
                for line in lines:
                    line = line.strip()
                    if not line or line.startswith('#'):
                        continue
                    if in_multiline_comment:
                        if '"""' in line or "'''" in line:
                            in_multiline_comment = False
                        continue
                    if line.startswith('"""') or line.startswith("'''"):
                        in_multiline_comment = True
                        continue
                    processed_lines.append(line)
            elif language == 'java':
                in_multiline_comment = False
                for line in lines:
                    line = line.strip()
                    if not line:
                        continue
                    if in_multiline_comment:
                        if '*/' in line:
                            in_multiline_comment = False
                        continue
                    if line.startswith('//'):
                        continue
                    if '/*' in line:
                        in_multiline_comment = True
                        continue
                    processed_lines.append(line)
            
            all_code.append((file_path, processed_lines))
        
        # Tìm kiếm các khối code trùng lặp
        min_lines = 5  # Số dòng tối thiểu để xem xét là đoạn trùng lặp
        duplicated_blocks = []
        total_lines = sum(len(lines) for _, lines in all_code)
        duplicated_lines = set()
        
        for i, (file1, lines1) in enumerate(all_code):
            for j, (file2, lines2) in enumerate(all_code):
                if i == j:  # Không so sánh file với chính nó
                    continue
                
                matches = difflib.SequenceMatcher(None, lines1, lines2).get_matching_blocks()
                for match in matches:
                    if match.size >= min_lines:
                        # Lưu block trùng lặp
                        duplicated_blocks.append({
                            'file1': file1,
                            'start1': match.a,
                            'file2': file2,
                            'start2': match.b,
                            'size': match.size,
                            'content': '\n'.join(lines1[match.a:match.a + match.size])
                        })
                        
                        # Đánh dấu các dòng bị trùng lặp
                        for k in range(match.size):
                            duplicated_lines.add((file1, match.a + k))
                            duplicated_lines.add((file2, match.b + k))
        
        # Tính phần trăm trùng lặp
        duplication_percentage = (len(duplicated_lines) / (2 * total_lines)) * 100 if total_lines > 0 else 0
        
        return {
            'duplication_percentage': duplication_percentage,
            'duplicated_blocks': duplicated_blocks
        }

    def measure_cohesion_coupling(self, code, language):
        """Đo lường Cohesion và Coupling"""
        if not code:
            return {
                'cohesion': 0,
                'coupling': 0,
                'details': {}
            }
            
        if language == 'python':
            try:
                # Sử dụng h_visit từ radon để đo lường cohesion
                h_metrics = h_visit(code)
                
                # Xây dựng đồ thị phụ thuộc để đo coupling
                tree = ast.parse(code)
                classes = {}
                imports = set()
                dependencies = defaultdict(set)
                
                # Thu thập classes và dependencies
                for node in ast.walk(tree):
                    if isinstance(node, ast.ClassDef):
                        classes[node.name] = {
                            'methods': [],
                            'attributes': [],
                            'dependencies': set()
                        }
                        
                        # Thu thập methods của class
                        for item in node.body:
                            if isinstance(item, ast.FunctionDef):
                                classes[node.name]['methods'].append(item.name)
                    
                    # Thu thập imports
                    elif isinstance(node, ast.Import):
                        for name in node.names:
                            imports.add(name.name)
                    elif isinstance(node, ast.ImportFrom):
                        if node.module:
                            imports.add(node.module)
                
                # Tính toán coupling - số lượng classes phụ thuộc vào class hiện tại
                coupling_score = len(imports) / len(classes) if classes else 0
                
                # Cohesion từ h_visit
                cohesion_values = [m.h_percent for m in h_metrics]
                cohesion_score = sum(cohesion_values) / len(cohesion_values) if cohesion_values else 0
                
                return {
                    'cohesion': cohesion_score / 100 if cohesion_score else 0,  # Chuyển về thang [0,1]
                    'coupling': min(coupling_score, 1.0),  # Giới hạn trong khoảng [0,1]
                    'details': {
                        'classes': len(classes),
                        'imports': len(imports),
                        'class_details': classes
                    }
                }
            except Exception as e:
                print(f"Lỗi khi phân tích cohesion & coupling cho Python: {e}")
                return {
                    'cohesion': 0,
                    'coupling': 0,
                    'details': {}
                }
                
        elif language == 'java':
            try:
                # Phân tích mã nguồn Java
                tokens = list(javalang.tokenizer.tokenize(code))
                parser = javalang.parser.Parser(tokens)
                tree = parser.parse()
                
                classes = {}
                imports = set()
                
                # Thu thập imports
                for path, node in tree.filter(javalang.tree.ImportDeclaration):
                    imports.add(node.path)
                
                # Thu thập classes và methods
                for path, node in tree.filter(javalang.tree.ClassDeclaration):
                    class_name = node.name
                    classes[class_name] = {
                        'methods': [],
                        'attributes': [],
                        'dependencies': set()
                    }
                    
                    # Thu thập methods
                    for method in node.methods:
                        classes[class_name]['methods'].append(method.name)
                
                # Tính toán coupling tương tự như với Python
                coupling_score = len(imports) / len(classes) if classes else 0
                
                # Tính toán cohesion đơn giản (tỷ lệ methods sử dụng các attributes)
                # Vì không có h_visit cho Java, sử dụng thước đo đơn giản
                method_count = sum(len(c['methods']) for c in classes.values())
                class_count = len(classes)
                
                cohesion_score = 0.7  # Giá trị mặc định vì không có công cụ đo cụ thể
                
                return {
                    'cohesion': cohesion_score,
                    'coupling': min(coupling_score, 1.0),
                    'details': {
                        'classes': len(classes),
                        'imports': len(imports),
                        'class_details': classes
                    }
                }
            except Exception as e:
                print(f"Lỗi khi phân tích cohesion & coupling cho Java: {e}")
                return {
                    'cohesion': 0,
                    'coupling': 0,
                    'details': {}
                }
    
    def generate_improvement_suggestions(self, metrics):
        """Tạo gợi ý cải thiện code dựa trên metrics"""
        suggestions = []
        
        # Dựa trên Cyclomatic Complexity
        if metrics.get('complexity', {}).get('average_complexity', 0) > 10:
            suggestions.append({
                'type': 'complexity',
                'severity': 'high',
                'message': 'Độ phức tạp Cyclomatic quá cao (> 10). Nên chia nhỏ các hàm phức tạp.'
            })
            
            # Tìm các hàm có độ phức tạp cao
            complex_functions = [f for f in metrics.get('complexity', {}).get('functions', []) 
                               if f.get('complexity', 0) > 15]
            if complex_functions:
                for func in complex_functions[:3]:  # Chỉ hiển thị 3 hàm phức tạp nhất
                    suggestions.append({
                        'type': 'complexity_detail',
                        'severity': 'high',
                        'message': f"Hàm '{func.get('name')}' có độ phức tạp là {func.get('complexity')}. "
                                  f"Cần refactor thành các hàm nhỏ hơn."
                    })
        
        # Dựa trên Code Duplication
        if metrics.get('duplication', {}).get('duplication_percentage', 0) > 10:
            suggestions.append({
                'type': 'duplication',
                'severity': 'medium',
                'message': f"Mức độ trùng lặp code cao ({metrics['duplication']['duplication_percentage']:.1f}%). "
                          f"Cân nhắc sử dụng các hàm chung hoặc tạo thư viện riêng."
            })
        
        # Dựa trên Cohesion & Coupling
        cohesion = metrics.get('cohesion_coupling', {}).get('cohesion', 0)
        coupling = metrics.get('cohesion_coupling', {}).get('coupling', 0)
        
        if cohesion < 0.5:
            suggestions.append({
                'type': 'cohesion',
                'severity': 'medium',
                'message': f"Mức độ cohesion thấp ({cohesion:.2f}). Các phương thức trong class không "
                          f"liên quan nhiều đến nhau. Cân nhắc tái cấu trúc các class."
            })
            
        if coupling > 0.7:
            suggestions.append({
                'type': 'coupling',
                'severity': 'high',
                'message': f"Mức độ coupling cao ({coupling:.2f}). Các class quá phụ thuộc vào nhau. "
                          f"Nên giảm sự phụ thuộc giữa các modules."
            })
        
        # Dựa trên LOC
        if metrics.get('loc', {}).get('code_lines', 0) > 500:
            suggestions.append({
                'type': 'loc',
                'severity': 'low',
                'message': f"File có quá nhiều dòng code ({metrics['loc']['code_lines']}). "
                          f"Cân nhắc chia thành nhiều file nhỏ hơn."
            })
            
        if metrics.get('loc', {}).get('comment_lines', 0) / max(metrics.get('loc', {}).get('code_lines', 1), 1) < 0.1:
            suggestions.append({
                'type': 'comments',
                'severity': 'low',
                'message': "Tỷ lệ comment thấp. Nên thêm comment để giải thích code phức tạp."
            })
            
        return suggestions
    
    def analyze_file(self, file_path):
        """Phân tích một file và tính toán tất cả metrics"""
        try:
            language = self.detect_language(file_path)
            if not language:
                return {
                    'error': f"Không hỗ trợ định dạng file: {file_path}"
                }
            
            code = self.load_file(file_path)
            if not code:
                return {
                    'error': f"Không thể đọc file: {file_path}"
                }
            
            # Tính toán tất cả metrics
            loc_metrics = self.measure_loc(code, language)
            complexity_metrics = self.measure_cyclomatic_complexity(code, language, file_path)
            cohesion_coupling = self.measure_cohesion_coupling(code, language)
            
            # Lưu kết quả
            file_metrics = {
                'language': language,
                'loc': loc_metrics,
                'complexity': complexity_metrics,
                'cohesion_coupling': cohesion_coupling
            }
            
            self.results[file_path] = file_metrics
            return file_metrics
            
        except Exception as e:
            print(f"Error in analyze_file: {str(e)}")  # Debug log
            return {
                'error': f"Lỗi khi phân tích file: {str(e)}"
            }
    
    def analyze_project(self, project_path):
        """Phân tích tất cả các file trong một dự án"""
        start_time = time.time()
        if not os.path.exists(project_path):
            return {
                'error': f"Đường dẫn không tồn tại: {project_path}"
            }
        
        self.results = {}
        files_content = {}
        
        # Duyệt qua tất cả các file trong dự án
        for root, _, files in os.walk(project_path):
            for file in files:
                file_path = os.path.join(root, file)
                language = self.detect_language(file_path)
                
                # Chỉ phân tích các file có ngôn ngữ được hỗ trợ
                if language in self.supported_languages:
                    self.analyze_file(file_path)
                    code = self.load_file(file_path)
                    if code:
                        files_content[file_path] = code
        
        # Tính toán các metrics toàn dự án
        project_metrics = self.calculate_project_metrics(files_content)
        
        # Tạo gợi ý cải thiện
        for file_path, metrics in self.results.items():
            suggestions = self.generate_improvement_suggestions(metrics)
            self.results[file_path]['suggestions'] = suggestions
        
        execution_time = time.time() - start_time
        
        return {
            'project_metrics': project_metrics,
            'file_metrics': self.results,
            'execution_time': execution_time
        }
        
    def calculate_project_metrics(self, files_content):
        """Tính toán metrics cho toàn bộ dự án"""
        if not files_content:
            return {
                'error': "Không có file nào được phân tích"
            }
        
        # Tạo danh sách ngôn ngữ được sử dụng
        languages = {}
        for file_path, metrics in self.results.items():
            lang = metrics.get('language')
            if lang:
                languages[lang] = languages.get(lang, 0) + 1
        
        # Tính metrics cho code duplication
        duplication_metrics = {}
        for lang in self.supported_languages:
            lang_files = {path: content for path, content in files_content.items() 
                        if self.results.get(path, {}).get('language') == lang}
            if lang_files:
                duplication_metrics[lang] = self.measure_duplication(lang_files, lang)
        
        # Tính cohesion & coupling cho từng file
        for file_path, content in files_content.items():
            language = self.results.get(file_path, {}).get('language')
            if language:
                cohesion_coupling = self.measure_cohesion_coupling(content, language)
                self.results[file_path]['cohesion_coupling'] = cohesion_coupling
        
        # Tính toán tổng các chỉ số
        total_loc = sum(m.get('loc', {}).get('code_lines', 0) for m in self.results.values())
        total_files = len(self.results)
        
        # Tính trung bình độ phức tạp
        complexity_values = [m.get('complexity', {}).get('average_complexity', 0) for m in self.results.values()]
        avg_complexity = sum(complexity_values) / len(complexity_values) if complexity_values else 0
        
        # Tính trung bình cohesion & coupling
        cohesion_values = [m.get('cohesion_coupling', {}).get('cohesion', 0) for m in self.results.values()]
        coupling_values = [m.get('cohesion_coupling', {}).get('coupling', 0) for m in self.results.values()]
        
        avg_cohesion = sum(cohesion_values) / len(cohesion_values) if cohesion_values else 0
        avg_coupling = sum(coupling_values) / len(coupling_values) if coupling_values else 0
        
        # Tính toán duplication trung bình
        duplication_values = [m.get('duplication_percentage', 0) for m in duplication_metrics.values()]
        avg_duplication = sum(duplication_values) / len(duplication_values) if duplication_values else 0
        
        return {
            'languages': languages,
            'total_files': total_files,
            'total_loc': total_loc,
            'average_complexity': avg_complexity,
            'average_cohesion': avg_cohesion,
            'average_coupling': avg_coupling,
            'average_duplication': avg_duplication,
            'duplication_metrics': duplication_metrics
        }
    
    def generate_complexity_chart(self):
        """Tạo biểu đồ phức tạp theo module"""
        if not self.results:
            return None
            
        # Thu thập dữ liệu cho biểu đồ
        modules = []
        complexities = []
        colors = []
        
        for file_path, metrics in self.results.items():
            module_name = os.path.basename(file_path)
            complexity = metrics.get('complexity', {}).get('average_complexity', 0)
            
            modules.append(module_name)
            complexities.append(complexity)
            
            # Màu sắc dựa trên độ phức tạp
            if complexity <= 5:
                colors.append('green')
            elif complexity <= 10:
                colors.append('orange')
            else:
                colors.append('red')
        
        # Tạo biểu đồ
        plt.figure(figsize=(10, 6))
        plt.bar(modules, complexities, color=colors)
        plt.xlabel('Module')
        plt.ylabel('Cyclomatic Complexity')
        plt.title('Cyclomatic Complexity theo Module')
        plt.xticks(rotation=45, ha='right')
        plt.tight_layout()
        
        # Lưu biểu đồ vào buffer
        buf = BytesIO()
        plt.savefig(buf, format='png')
        buf.seek(0)
        
        # Chuyển sang base64 để nhúng vào HTML
        data = base64.b64encode(buf.getvalue()).decode('utf-8')
        plt.close()
        
        return f"data:image/png;base64,{data}"
    
    def generate_cohesion_coupling_chart(self):
        """Tạo biểu đồ nhiệt cho Cohesion & Coupling"""
        if not self.results:
            return None
            
        # Thu thập dữ liệu
        modules = []
        cohesion_values = []
        coupling_values = []
        
        for file_path, metrics in self.results.items():
            module_name = os.path.basename(file_path)
            cohesion = metrics.get('cohesion_coupling', {}).get('cohesion', 0)
            coupling = metrics.get('cohesion_coupling', {}).get('coupling', 0)
            
            modules.append(module_name)
            cohesion_values.append(cohesion)
            coupling_values.append(coupling)
        
        # Tạo DataFrame cho heatmap
        data = {
            'Module': modules,
            'Cohesion': cohesion_values,
            'Coupling': coupling_values
        }
        df = pd.DataFrame(data)
        
        # Pivot cho heatmap
        matrix = df.pivot_table(index='Module', values=['Cohesion', 'Coupling'])
        
        # Tạo heatmap
        plt.figure(figsize=(12, 8))
        sns.heatmap(matrix, annot=True, cmap='coolwarm', linewidths=.5)
        plt.title('Cohesion & Coupling theo Module')
        plt.tight_layout()
        
        # Lưu biểu đồ vào buffer
        buf = BytesIO()
        plt.savefig(buf, format='png')
        buf.seek(0)
        
        # Chuyển sang base64
        data = base64.b64encode(buf.getvalue()).decode('utf-8')
        plt.close()
        
        return f"data:image/png;base64,{data}"

# Thiết lập các routes Flask
@app.route('/')
def index():
    return render_template('index.html')

@app.route('/browse', methods=['POST'])
def browse():
    mode = request.form.get('mode', 'file')
    
    root = tk.Tk()
    root.withdraw()  # Ẩn cửa sổ chính của Tkinter
    root.attributes('-topmost', True)  # Đảm bảo dialog hiển thị phía trên
    
    try:
        if mode == 'file':
            file_path = filedialog.askopenfilename(
                title='Chọn file để phân tích',
                filetypes=[
                    ('Python files', '*.py'),
                    ('Java files', '*.java'),
                    ('All files', '*.*')
                ]
            )
            if file_path:
                # Chuyển đổi đường dẫn sang định dạng chuẩn
                file_path = os.path.normpath(file_path)
                return jsonify({'path': file_path})
        else:
            dir_path = filedialog.askdirectory(
                title='Chọn thư mục dự án để phân tích',
                mustexist=True  # Đảm bảo thư mục phải tồn tại
            )
            if dir_path:
                # Chuyển đổi đường dẫn sang định dạng chuẩn
                dir_path = os.path.normpath(dir_path)
                return jsonify({'path': dir_path})
                
        return jsonify({'error': 'Không có đường dẫn nào được chọn'})
        
    except Exception as e:
        return jsonify({'error': str(e)})
    finally:
        root.destroy()  # Đảm bảo cửa sổ Tkinter được đóng hoàn toàn

@app.route('/analyze', methods=['POST'])
def analyze_route():
    try:
        uploaded_file = request.files.get('file')
        mode = request.form.get('analysis_mode', 'file')

        if not uploaded_file:
            return jsonify({'error': 'Không có file được tải lên'})

        # Lưu file tạm để phân tích
        temp_dir = 'uploads'
        os.makedirs(temp_dir, exist_ok=True)
        temp_path = os.path.join(temp_dir, uploaded_file.filename)
        uploaded_file.save(temp_path)

        analyzer = CodeMetricsAnalyzer()

        if mode == 'file':
            results = analyzer.analyze_file(temp_path)
        else:
            results = analyzer.analyze_project(temp_path)

        # Xóa file sau khi xử lý nếu cần
        os.remove(temp_path)

        if 'error' in results:
            return jsonify({'error': results['error']})

        # Tạo biểu đồ
        complexity_chart = analyzer.generate_complexity_chart()
        cohesion_coupling_chart = analyzer.generate_cohesion_coupling_chart()

        return jsonify({
            'results': results,
            'charts': {
                'complexity_chart': complexity_chart,
                'cohesion_coupling_chart': cohesion_coupling_chart
            }
        })

    except Exception as e:
        print(f"Error in analyze_route: {str(e)}")
        return jsonify({'error': f'Lỗi khi phân tích: {str(e)}'})
    
@app.route('/suggestions/<path:file_path>')
def get_suggestions(file_path):
    analyzer = CodeMetricsAnalyzer()
    file_metrics = analyzer.analyze_file(file_path)
    suggestions = analyzer.generate_improvement_suggestions(file_metrics)
    
    return jsonify({
        'file_path': file_path,
        'suggestions': suggestions
    })

if __name__ == '__main__':  
    # Chạy ứng dụng
    app.run(debug=True)