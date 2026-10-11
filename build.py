# -*- coding: utf-8 -*-
"""
dsh-float 0.7.31 beta C# 编译工具
- 一键调用 csc.exe 编译 C# 项目
- 深色主题 PyQt6
- 日志可一键复制
"""
import os
import sys
import shutil
import subprocess
from pathlib import Path

from PyQt6.QtCore import Qt, QThread, pyqtSignal
from PyQt6.QtGui import QFont, QIcon, QTextCursor
from PyQt6.QtWidgets import (
    QApplication, QMainWindow, QWidget, QVBoxLayout, QHBoxLayout,
    QPushButton, QTextEdit, QLabel, QMessageBox
)


APP_TITLE    = "dsh-float 编译工具 0.7.31 beta"
APP_VERSION  = "0.7.31"
APP_BUILD    = "beta"

PROJECT_ROOT = Path(__file__).parent.resolve()
SRC_DIR      = PROJECT_ROOT / "src"
SDK_DIR      = PROJECT_ROOT / "sdk"
RES_DIR      = PROJECT_ROOT / "res"
OUTPUT_DIR   = PROJECT_ROOT / "build"


DARK_QSS = """
QMainWindow, QWidget {
    background-color: #1e1e1e;
    color: #e0e0e0;
}
QLabel {
    color: #e0e0e0;
    font-family: "Microsoft YaHei";
    font-size: 13px;
}
QPushButton {
    background-color: #2d2d30;
    color: #e0e0e0;
    border: 1px solid #3e3e42;
    border-radius: 6px;
    padding: 8px 16px;
    font-family: "Microsoft YaHei";
    font-size: 13px;
    min-height: 20px;
}
QPushButton:hover {
    background-color: #3e3e42;
    border-color: #007acc;
}
QPushButton:pressed {
    background-color: #007acc;
    color: #ffffff;
}
QPushButton:disabled {
    background-color: #252526;
    color: #6a6a6a;
    border-color: #333336;
}
QPushButton#primary {
    background-color: #007acc;
    color: #ffffff;
    border: none;
}
QPushButton#primary:hover {
    background-color: #1a8fd6;
}
QPushButton#primary:pressed {
    background-color: #005a9e;
}
QTextEdit {
    background-color: #0d0d0d;
    color: #d4d4d4;
    border: 1px solid #3e3e42;
    border-radius: 6px;
    padding: 8px;
    font-family: "Consolas", "Courier New", monospace;
    font-size: 12px;
    selection-background-color: #264f78;
}
QTextEdit:focus {
    border-color: #007acc;
}
"""


class BuildWorker(QThread):
    log_signal  = pyqtSignal(str)
    done_signal = pyqtSignal(int)

    def __init__(self):
        super().__init__()
        self._stop = False

    def stop(self):
        self._stop = True

    def log(self, msg):
        self.log_signal.emit(msg)

    def run(self):
        try:
            self._do_build()
        except Exception as e:
            self.log("\n[异常] " + str(e))
            self.done_signal.emit(-1)

    def _do_build(self):
        self.log("dsh-float 编译工具 " + APP_VERSION + " " + APP_BUILD)
        self.log("=" * 60)

        csc = self._find_csc()
        if not csc:
            self.log("[错误] 找不到 csc.exe")
            self.log("请确认系统已安装 .NET Framework 4.x")
            self.done_signal.emit(-1)
            return
        self.log("[OK] csc.exe: " + csc)

        core_dll  = SDK_DIR / "Microsoft.Web.WebView2.Core.dll"
        forms_dll = SDK_DIR / "Microsoft.Web.WebView2.WinForms.dll"
        loader_dll = SDK_DIR / "WebView2Loader.dll"

        if not core_dll.exists() or not forms_dll.exists():
            self.log("[错误] 缺少 WebView2 SDK")
            self.log("请把以下文件放到 " + str(SDK_DIR))
            self.log("  Microsoft.Web.WebView2.Core.dll")
            self.log("  Microsoft.Web.WebView2.WinForms.dll")
            self.done_signal.emit(-1)
            return
        self.log("[OK] WebView2 SDK 已就绪")

        cs_files = [SRC_DIR / "Main.cs"]
        if not cs_files:
            self.log("[错误] " + str(SRC_DIR) + " 下没有 .cs 文件")
            self.done_signal.emit(-1)
            return
        self.log("[OK] 找到 " + str(len(cs_files)) + " 个 .cs 文件")
        for f in cs_files:
            self.log("      " + f.name)

        OUTPUT_DIR.mkdir(exist_ok=True)
        out_exe = OUTPUT_DIR / "dsh-float.exe"

        csc_args = [
            str(csc),
            "/target:winexe",
            "/platform:x64",
            "/optimize+",
            "/out:" + str(out_exe),
            "/reference:System.dll",
            "/reference:System.Windows.Forms.dll",
            "/reference:System.Drawing.dll",
            "/reference:System.Web.Extensions.dll",
            "/reference:C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319\\System.IO.Compression.dll",
            "/reference:C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319\\System.IO.Compression.FileSystem.dll",
            "/reference:System.Net.Http.dll",
            "/reference:" + str(core_dll),
            "/reference:" + str(forms_dll),
        ]

        icon = RES_DIR / "favicon.ico"
        if icon.exists():
            csc_args.append("/win32icon:" + str(icon))
            self.log("[OK] 图标：" + str(icon))
        else:
            self.log("[跳过] 无图标：" + str(icon))

        for f in cs_files:
            csc_args.append(str(f))

        self.log("")
        self.log("=" * 60)
        self.log("执行编译：")
        self.log("  " + " ".join(csc_args))
        self.log("=" * 60)
        self.log("")

        proc = subprocess.Popen(
            csc_args,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True,
            encoding="utf-8",
            errors="replace",
            creationflags=subprocess.CREATE_NO_WINDOW,
        )
        for line in proc.stdout:
            if self._stop:
                proc.kill()
                self.log("[取消] 编译已中断")
                self.done_signal.emit(-1)
                return
            self.log(line.rstrip())
        proc.wait()

        if proc.returncode != 0:
            self.log("\n[失败] csc.exe 退出码 " + str(proc.returncode))
            self.done_signal.emit(proc.returncode)
            return

        if loader_dll.exists():
            shutil.copy2(loader_dll, OUTPUT_DIR / loader_dll.name)
            self.log("[OK] 已拷贝 " + loader_dll.name)
        else:
            self.log("[警告] 缺少 " + loader_dll.name)

        for res in RES_DIR.glob("*"):
            if res.is_file():
                shutil.copy2(res, OUTPUT_DIR / res.name)
                self.log("[OK] 已拷贝 " + res.name)

        self.log("")
        self.log("=" * 60)
        self.log("✅ 编译成功！")
        self.log("   版本：dsh-float " + APP_VERSION + " " + APP_BUILD)
        self.log("   输出：" + str(out_exe))
        self.log("=" * 60)
        self.done_signal.emit(0)

    def _find_csc(self):
        candidates = [
            r"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
            r"C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe",
        ]
        for p in candidates:
            if os.path.exists(p):
                return p
        return None


class MainWindow(QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle(APP_TITLE)
        self.resize(960, 660)
        self.worker = None
        self._build_ui()

    def _build_ui(self):
        central = QWidget()
        self.setCentralWidget(central)
        layout = QVBoxLayout(central)
        layout.setContentsMargins(16, 16, 16, 16)
        layout.setSpacing(12)

        top = QHBoxLayout()
        self.btn_build = QPushButton("🔨 开始编译")
        self.btn_build.setObjectName("primary")
        self.btn_build.clicked.connect(self.start_build)

        self.btn_clean = QPushButton("🧹 清理输出")
        self.btn_clean.clicked.connect(self.clean_output)

        self.btn_run = QPushButton("▶ 运行 exe")
        self.btn_run.clicked.connect(self.run_exe)

        self.btn_open = QPushButton("📂 打开输出目录")
        self.btn_open.clicked.connect(self.open_output)

        top.addWidget(self.btn_build)
        top.addWidget(self.btn_clean)
        top.addWidget(self.btn_run)
        top.addWidget(self.btn_open)
        top.addStretch()
        layout.addLayout(top)

        self.lbl_status = QLabel("就绪 —— dsh-float " + APP_VERSION + " " + APP_BUILD)
        layout.addWidget(self.lbl_status)

        self.log_view = QTextEdit()
        self.log_view.setReadOnly(True)
        self.log_view.setFont(QFont("Consolas", 10))
        layout.addWidget(self.log_view, 1)

        bottom = QHBoxLayout()
        self.btn_copy = QPushButton("📋 一键复制日志")
        self.btn_copy.clicked.connect(self.copy_log)

        self.btn_clear = QPushButton("🗑 清空日志")
        self.btn_clear.clicked.connect(lambda: self.log_view.clear())

        bottom.addWidget(self.btn_copy)
        bottom.addWidget(self.btn_clear)
        bottom.addStretch()
        layout.addLayout(bottom)

    def log(self, msg):
        self.log_view.append(msg)
        cursor = self.log_view.textCursor()
        cursor.movePosition(QTextCursor.MoveOperation.End)
        self.log_view.setTextCursor(cursor)

    def start_build(self):
        if self.worker and self.worker.isRunning():
            QMessageBox.information(self, "提示", "编译正在进行中...")
            return
        self.log_view.clear()
        self.lbl_status.setText("编译中...")
        self.btn_build.setEnabled(False)

        self.worker = BuildWorker()
        self.worker.log_signal.connect(self.log)
        self.worker.done_signal.connect(self.on_build_done)
        self.worker.start()

    def on_build_done(self, code):
        self.btn_build.setEnabled(True)
        if code == 0:
            self.lbl_status.setText("✅ 编译成功 —— dsh-float " + APP_VERSION + " " + APP_BUILD)
        else:
            self.lbl_status.setText("❌ 编译失败（退出码 " + str(code) + "）")

    def clean_output(self):
        if OUTPUT_DIR.exists():
            try:
                shutil.rmtree(OUTPUT_DIR)
                self.log("[清理] 已删除 " + str(OUTPUT_DIR))
                self.lbl_status.setText("已清理")
            except Exception as e:
                QMessageBox.warning(self, "清理失败", str(e))
        else:
            self.log("[清理] 输出目录不存在")

    def run_exe(self):
        exe = OUTPUT_DIR / "dsh-float.exe"
        if not exe.exists():
            QMessageBox.warning(self, "提示", "exe 不存在，请先编译")
            return
        try:
            subprocess.Popen([str(exe)], cwd=str(OUTPUT_DIR))
            self.log("[运行] " + str(exe))
        except Exception as e:
            QMessageBox.critical(self, "运行失败", str(e))

    def open_output(self):
        OUTPUT_DIR.mkdir(exist_ok=True)
        try:
            os.startfile(str(OUTPUT_DIR))
        except Exception as e:
            QMessageBox.warning(self, "打开失败", str(e))

    def copy_log(self):
        text = self.log_view.toPlainText()
        if not text:
            QMessageBox.information(self, "提示", "日志为空")
            return
        QApplication.clipboard().setText(text)
        self.lbl_status.setText("📋 日志已复制到剪贴板")


def main():
    app = QApplication(sys.argv)
    app.setStyleSheet(DARK_QSS)
    win = MainWindow()
    win.show()
    sys.exit(app.exec())


if __name__ == "__main__":
    main()