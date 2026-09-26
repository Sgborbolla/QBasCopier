namespace QBasCopierSetup;

public static class SetupLoc
{
    public static readonly string[] Codes =
    {
        "es", "en", "fr", "pt", "it", "de", "nl", "ru", "uk", "pl",
        "tr", "cs", "ro", "hi", "ar", "zh", "ja", "ko", "id", "vi"
    };

    public static readonly string[] Native =
    {
        "Español", "English", "Français", "Português", "Italiano", "Deutsch",
        "Nederlands", "Русский", "Українська", "Polski", "Türkçe", "Čeština",
        "Română", "हिन्दी", "العربية", "中文", "日本語", "한국어", "Bahasa Indonesia", "Tiếng Việt"
    };

    // índices: 0 título, 1 idioma, 2 destino, 3 examinar, 4 opciones, 5 instalar,
    // 6 instalando, 7 listo, 8 lanzar, 9 cerrar, 10 integrar, 11 inicio, 12 escritorio, 13 bienvenida
    private static readonly string[][] T =
    {
        new[] {"Instalador de QBasCopier", "Idioma a instalar:", "Carpeta de instalación:", "Examinar…", "Opciones", "Instalar", "Instalando…", "Instalación completada", "Ejecutar QBasCopier", "Cerrar", "Integrar en el Explorador de Windows y menú contextual (Total Commander, etc.)", "Iniciar con Windows", "Crear acceso directo en el escritorio", "Bienvenido(a) al instalador de QBasCopier."},
        new[] {"QBasCopier-y-Transfer Setup", "Language to install:", "Installation folder:", "Browse…", "Options", "Install", "Installing…", "Installation completed", "Run QBasCopier y Transfer", "Close", "Integrate into Windows Explorer and context menus (Total Commander, etc.)", "Start with Windows", "Create desktop shortcut", "Welcome to the QBasCopier y Transfer installer."},
        new[] {"Installateur QBasCopier", "Langue à installer :", "Dossier d'installation :", "Parcourir…", "Options", "Installer", "Installation…", "Installation terminée", "Lancer QBasCopier", "Fermer", "Intégrer à l'Explorateur Windows et aux menus contextuels (Total Commander, etc.)", "Démarrer avec Windows", "Créer un raccourci bureau", "Bienvenue dans l'installateur de QBasCopier."},
        new[] {"Instalador do QBasCopier", "Idioma a instalar:", "Pasta de instalação:", "Procurar…", "Opções", "Instalar", "Instalando…", "Instalação concluída", "Executar QBasCopier", "Fechar", "Integrar ao Explorador do Windows e menus de contexto (Total Commander, etc.)", "Iniciar com o Windows", "Criar atalho na área de trabalho", "Bem-vindo ao instalador do QBasCopier."},
        new[] {"Installazione di QBasCopier", "Lingua da installare:", "Cartella di installazione:", "Sfoglia…", "Opzioni", "Installa", "Installazione…", "Installazione completata", "Esegui QBasCopier", "Chiudi", "Integra in Esplora file di Windows e menu contestuali (Total Commander, ecc.)", "Avvia con Windows", "Crea collegamento sul desktop", "Benvenuto nell'installatore di QBasCopier."},
        new[] {"QBasCopier-Setup", "Zu installierende Sprache:", "Installationsordner:", "Durchsuchen…", "Optionen", "Installieren", "Installiere…", "Installation abgeschlossen", "QBasCopier starten", "Schließen", "In Windows-Explorer und Kontextmenü integrieren (Total Commander usw.)", "Mit Windows starten", "Desktopverknüpfung erstellen", "Willkommen beim QBasCopier-Installer."},
        new[] {"QBasCopier Installatie", "Te installeren taal:", "Installatiemap:", "Bladeren…", "Opties", "Installeren", "Bezig met installeren…", "Installatie voltooid", "QBasCopier uitvoeren", "Sluiten", "Integreren in Windows Verkenner en contextmenu's (Total Commander, enz.)", "Start met Windows", "Snelkoppeling op bureaublad maken", "Welkom bij de QBasCopier-installatie."},
        new[] {"Установщик QBasCopier", "Язык установки:", "Папка установки:", "Обзор…", "Параметры", "Установить", "Установка…", "Установка завершена", "Запустить QBasCopier", "Закрыть", "Интегрировать в Проводник Windows и контекстные меню (Total Commander и др.)", "Запуск с Windows", "Создать ярлык на рабочем столе", "Добро пожаловать в установщик QBasCopier."},
        new[] {"Встановлювач QBasCopier", "Мова для встановлення:", "Папка встановлення:", "Огляд…", "Параметри", "Встановити", "Встановлення…", "Встановлення завершено", "Запустити QBasCopier", "Закрити", "Інтегрувати в Провідник Windows і контекстні меню (Total Commander тощо)", "Запуск із Windows", "Створити ярлик на робочому столі", "Ласкаво просимо до встановлювача QBasCopier."},
        new[] {"Instalator QBasCopier", "Język instalacji:", "Folder instalacji:", "Przeglądaj…", "Opcje", "Zainstaluj", "Instalowanie…", "Instalacja zakończona", "Uruchom QBasCopier", "Zamknij", "Zintegruj z Eksploratorem Windows i menu kontekstowym (Total Commander itp.)", "Uruchom z Windows", "Utwórz skrót na pulpicie", "Witamy w instalatorze QBasCopier."},
        new[] {"QBasCopier Kurulumu", "Yüklenecek dil:", "Kurulum klasörü:", "Gözat…", "Seçenekler", "Kur", "Kuruluyor…", "Kurulum tamamlandı", "QBasCopier'i çalıştır", "Kapat", "Windows Gezgini ve bağlam menülerine (Total Commander vb.) entegre et", "Windows ile başlat", "Masaüstü kısayolu oluştur", "QBasCopier kurulumuna hoş geldiniz."},
        new[] {"Instalátor QBasCopier", "Jazyk instalace:", "Instalační složka:", "Procházet…", "Možnosti", "Nainstalovat", "Instaluji…", "Instalace dokončena", "Spustit QBasCopier", "Zavřít", "Integrovat do Průzkumníka Windows a kontextových nabídek (Total Commander atd.)", "Spustit s Windows", "Vytvořit zástupce na ploše", "Vítejte v instalátoru QBasCopier."},
        new[] {"Instalator QBasCopier", "Limbă de instalat:", "Folder de instalare:", "Răsfoiește…", "Opțiuni", "Instalează", "Se instalează…", "Instalare finalizată", "Rulează QBasCopier", "Închide", "Integrează în Explorer Windows și meniurile contextuale (Total Commander etc.)", "Pornește cu Windows", "Creează scurtătură pe desktop", "Bine ați venit la instalatorul QBasCopier."},
        new[] {"QBasCopier इंस्टॉलर", "स्थापित करने की भाषा:", "स्थापना फ़ोल्डर:", "ब्राउज़ करें…", "विकल्प", "स्थापित करें", "स्थापित हो रहा है…", "स्थापना पूर्ण", "QBasCopier चलाएँ", "बंद करें", "Windows Explorer और संदर्भ मेनू (Total Commander आदि) में एकीकृत करें", "Windows के साथ प्रारंभ", "डेस्कटॉप शॉर्टकट बनाएँ", "QBasCopier इंस्टॉलर में आपका स्वागत है।"},
        new[] {"مثبت QBasCopier", "لغة التثبيت:", "مجلد التثبيت:", "استعراض…", "خيارات", "تثبيت", "جارٍ التثبيت…", "اكتمل التثبيت", "تشغيل QBasCopier", "إغلاق", "الدمج في مستكشف Windows وقوائم السياق (Total Commander وغيرها)", "بدء مع Windows", "إنشاء اختصار على سطح المكتب", "مرحباً بك في مثبت QBasCopier."},
        new[] {"QBasCopier 安装程序", "安装语言：", "安装文件夹：", "浏览…", "选项", "安装", "正在安装…", "安装完成", "运行 QBasCopier", "关闭", "集成到 Windows 资源管理器和右键菜单（Total Commander 等）", "随 Windows 启动", "创建桌面快捷方式", "欢迎使用 QBasCopier 安装程序。"},
        new[] {"QBasCopier インストーラー", "インストールする言語：", "インストール先フォルダー：", "参照…", "オプション", "インストール", "インストール中…", "インストール完了", "QBasCopier を実行", "閉じる", "Windows エクスプローラーとコンテキストメニューに統合（Total Commander など）", "Windows とともに起動", "デスクトップにショートカットを作成", "QBasCopier インストーラーへようこそ。"},
        new[] {"QBasCopier 설치", "설치할 언어:", "설치 폴더:", "찾아보기…", "옵션", "설치", "설치 중…", "설치 완료", "QBasCopier 실행", "닫기", "Windows 탐색기 및 컨텍스트 메뉴에 통합(Total Commander 등)", "Windows와 함께 시작", "바탕 화면 바로 가기 만들기", "QBasCopier 설치 프로그램에 오신 것을 환영합니다."},
        new[] {"Penginstal QBasCopier", "Bahasa yang akan diinstal:", "Folder instalasi:", "Jelajahi…", "Opsi", "Instal", "Menginstal…", "Instalasi selesai", "Jalankan QBasCopier", "Tutup", "Integrasikan ke Windows Explorer dan menu konteks (Total Commander, dll.)", "Mulai dengan Windows", "Buat pintasan di desktop", "Selamat datang di penginstal QBasCopier."},
        new[] {"Trình cài đặt QBasCopier", "Ngôn ngữ cần cài đặt:", "Thư mục cài đặt:", "Duyệt…", "Tùy chọn", "Cài đặt", "Đang cài đặt…", "Cài đặt hoàn tất", "Chạy QBasCopier", "Đóng", "Tích hợp vào Windows Explorer và menu ngữ cảnh (Total Commander, v.v.)", "Khởi động cùng Windows", "Tạo lối tắt trên màn hình nền", "Chào mừng bạn đến với trình cài đặt QBasCopier."},
    };

    public static int Current = 0;

    public static string Get(int i) => T[Current][i];
}