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
        new[] {"Integrar en el Explorador de Windows y el menú contextual", "Idioma a instalar:", "Carpeta de instalación:", "Examinar…", "Opciones", "Instalar", "Instalando…", "Instalación completada", "Ejecutar QBasCopier", "Cerrar", "Integrar en el Explorador de Windows y el menú contextual", "Iniciar con Windows", "Crear acceso directo en el escritorio", "Bienvenido(a) al instalador de QBasCopier."},
        new[] {"Integrate into Windows Explorer and the context menu", "Language to install:", "Installation folder:", "Browse…", "Options", "Install", "Installing…", "Installation completed", "Run QBasCopier y Transfer", "Close", "Integrate into Windows Explorer and the context menu", "Start with Windows", "Create desktop shortcut", "Welcome to the QBasCopier y Transfer installer."},
        new[] {"Intégrer à l’Explorateur Windows et au menu contextuel", "Langue à installer :", "Dossier d'installation :", "Parcourir…", "Options", "Installer", "Installation…", "Installation terminée", "Lancer QBasCopier", "Fermer", "Intégrer à l’Explorateur Windows et au menu contextuel", "Démarrer avec Windows", "Créer un raccourci bureau", "Bienvenue dans l'installateur de QBasCopier."},
        new[] {"Integrar ao Explorador do Windows e ao menu de contexto", "Idioma a instalar:", "Pasta de instalação:", "Procurar…", "Opções", "Instalar", "Instalando…", "Instalação concluída", "Executar QBasCopier", "Fechar", "Integrar ao Explorador do Windows e ao menu de contexto", "Iniciar com o Windows", "Criar atalho na área de trabalho", "Bem-vindo ao instalador do QBasCopier."},
        new[] {"Integra in Esplora File e nei menu contestuali", "Lingua da installare:", "Cartella di installazione:", "Sfoglia…", "Opzioni", "Installa", "Installazione…", "Installazione completata", "Esegui QBasCopier", "Chiudi", "Integra in Esplora File e nei menu contestuali", "Avvia con Windows", "Crea collegamento sul desktop", "Benvenuto nell'installatore di QBasCopier."},
        new[] {"In Windows-Explorer und im Kontextmenü integrieren", "Zu installierende Sprache:", "Installationsordner:", "Durchsuchen…", "Optionen", "Installieren", "Installiere…", "Installation abgeschlossen", "QBasCopier starten", "Schließen", "In Windows-Explorer und im Kontextmenü integrieren", "Mit Windows starten", "Desktopverknüpfung erstellen", "Willkommen beim QBasCopier-Installer."},
        new[] {"Integreren in Windows Verkenner en het contextmenu", "Te installeren taal:", "Installatiemap:", "Bladeren…", "Opties", "Installeren", "Bezig met installeren…", "Installatie voltooid", "QBasCopier uitvoeren", "Sluiten", "Integreren in Windows Verkenner en het contextmenu", "Start met Windows", "Snelkoppeling op bureaublad maken", "Welkom bij de QBasCopier-installatie."},
        new[] {"Интегрировать в Проводник и контекстное меню", "Язык установки:", "Папка установки:", "Обзор…", "Параметры", "Установить", "Установка…", "Установка завершена", "Запустить QBasCopier", "Закрыть", "Интегрировать в Проводник и контекстное меню", "Запуск с Windows", "Создать ярлык на рабочем столе", "Добро пожаловать в установщик QBasCopier."},
        new[] {"Інтегрувати в Провідник і контекстне меню", "Мова для встановлення:", "Папка встановлення:", "Огляд…", "Параметри", "Встановити", "Встановлення…", "Встановлення завершено", "Запустити QBasCopier", "Закрити", "Інтегрувати в Провідник і контекстне меню", "Запуск із Windows", "Створити ярлик на робочому столі", "Ласкаво просимо до встановлювача QBasCopier."},
        new[] {"Zintegruj z Eksploratorem plików i menu kontekstowym", "Język instalacji:", "Folder instalacji:", "Przeglądaj…", "Opcje", "Zainstaluj", "Instalowanie…", "Instalacja zakończona", "Uruchom QBasCopier", "Zamknij", "Zintegruj z Eksploratorem plików i menu kontekstowym", "Uruchom z Windows", "Utwórz skrót na pulpicie", "Witamy w instalatorze QBasCopier."},
        new[] {"Windows Gezgini ve sağ tık menüsüne tümleştir", "Yüklenecek dil:", "Kurulum klasörü:", "Gözat…", "Seçenekler", "Kur", "Kuruluyor…", "Kurulum tamamlandı", "QBasCopier'i çalıştır", "Kapat", "Windows Gezgini ve sağ tık menüsüne tümleştir", "Windows ile başlat", "Masaüstü kısayolu oluştur", "QBasCopier kurulumuna hoş geldiniz."},
        new[] {"Integrovat do Průzkumníka a kontextové nabídky", "Jazyk instalace:", "Instalační složka:", "Procházet…", "Možnosti", "Nainstalovat", "Instaluji…", "Instalace dokončena", "Spustit QBasCopier", "Zavřít", "Integrovat do Průzkumníka a kontextové nabídky", "Spustit s Windows", "Vytvořit zástupce na ploše", "Vítejte v instalátoru QBasCopier."},
        new[] {"Instalator QBasCopier", "Limbă de instalat:", "Folder de instalare:", "Răsfoiește…", "Opțiuni", "Instalează", "Se instalează…", "Instalare finalizată", "Rulează QBasCopier", "Închide", "Integrare în Explorator de fișiere și meniul contextual", "Pornește cu Windows", "Creează scurtătură pe desktop", "Bine ați venit la instalatorul QBasCopier."},
        new[] {"QBasCopier इंस्टॉलर", "स्थापित करने की भाषा:", "स्थापना फ़ोल्डर:", "ब्राउज़ करें…", "विकल्प", "स्थापित करें", "स्थापित हो रहा है…", "स्थापना पूर्ण", "QBasCopier चलाएँ", "बंद करें", "Windows फ़ाइल एक्सप्लोरर और संदर्भ मेन्यू में एकीकृत करें", "Windows के साथ प्रारंभ", "डेस्कटॉप शॉर्टकट बनाएँ", "QBasCopier इंस्टॉलर में आपका स्वागत है।"},
        new[] {"Integrare în Explorator de fișiere și meniul contextual", "لغة التثبيت:", "مجلد التثبيت:", "استعراض…", "خيارات", "تثبيت", "جارٍ التثبيت…", "اكتمل التثبيت", "تشغيل QBasCopier", "إغلاق", "دمج في مستكشف Windows وقائمة السياق", "بدء مع Windows", "إنشاء اختصار على سطح المكتب", "مرحباً بك في مثبت QBasCopier."},
        new[] {"Windows फ़ाइल एक्सप्लोरर और संदर्भ मेन्यू में एकीकृत करें", "安装语言：", "安装文件夹：", "浏览…", "选项", "安装", "正在安装…", "安装完成", "运行 QBasCopier", "关闭", "集成到 Windows 资源管理器和右键菜单", "随 Windows 启动", "创建桌面快捷方式", "欢迎使用 QBasCopier 安装程序。"},
        new[] {"دمج في مستكشف Windows وقائمة السياق", "インストールする言語：", "インストール先フォルダー：", "参照…", "オプション", "インストール", "インストール中…", "インストール完了", "QBasCopier を実行", "閉じる", "Windows エクスプローラーとコンテキストメニューに統合", "Windows とともに起動", "デスクトップにショートカットを作成", "QBasCopier インストーラーへようこそ。"},
        new[] {"集成到 Windows 资源管理器和右键菜单", "설치할 언어:", "설치 폴더:", "찾아보기…", "옵션", "설치", "설치 중…", "설치 완료", "QBasCopier 실행", "닫기", "Windows 탐색기 및 컨텍스트 메뉴에 통합", "Windows와 함께 시작", "바탕 화면 바로 가기 만들기", "QBasCopier 설치 프로그램에 오신 것을 환영합니다."},
        new[] {"Windows エクスプローラーとコンテキストメニューに統合", "Bahasa yang akan diinstal:", "Folder instalasi:", "Jelajahi…", "Opsi", "Instal", "Menginstal…", "Instalasi selesai", "Jalankan QBasCopier", "Tutup", "Integrasikan ke Windows Explorer dan menu konteks", "Mulai dengan Windows", "Buat pintasan di desktop", "Selamat datang di penginstal QBasCopier."},
        new[] {"Windows 탐색기 및 컨텍스트 메뉴에 통합", "Ngôn ngữ cần cài đặt:", "Thư mục cài đặt:", "Duyệt…", "Tùy chọn", "Cài đặt", "Đang cài đặt…", "Cài đặt hoàn tất", "Chạy QBasCopier", "Đóng", "Tích hợp vào Windows Explorer và menu ngữ cảnh", "Khởi động cùng Windows", "Tạo lối tắt trên màn hình nền", "Chào mừng bạn đến với trình cài đặt QBasCopier."},
    };

    public static int Current = 0;

    public static string Get(int i) => T[Current][i];
}