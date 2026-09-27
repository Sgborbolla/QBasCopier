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
        new[] {"Integrar en el Explorador de Windows y el menú contextual", "Idioma a instalar:", "Carpeta de instalación:", "Examinar…", "Opciones", "Instalar", "Instalando…", "Instalación completada", "Ejecutar QBasWing Shuttle", "Cerrar", "Integrar en el Explorador de Windows y el menú contextual", "Iniciar con Windows", "Crear acceso directo en el escritorio", "Bienvenido(a) al instalador de QBasWing Shuttle."},
        new[] {"Integrate into Windows Explorer and the context menu", "Language to install:", "Installation folder:", "Browse…", "Options", "Install", "Installing…", "Installation completed", "Run QBasWing Shuttle", "Close", "Integrate into Windows Explorer and the context menu", "Start with Windows", "Create desktop shortcut", "Welcome to the QBasWing Shuttle installer."},
        new[] {"Intégrer à l’Explorateur Windows et au menu contextuel", "Langue à installer :", "Dossier d'installation :", "Parcourir…", "Options", "Installer", "Installation…", "Installation terminée", "Lancer QBasWing Shuttle", "Fermer", "Intégrer à l’Explorateur Windows et au menu contextuel", "Démarrer avec Windows", "Créer un raccourci bureau", "Bienvenue dans l'installateur de QBasWing Shuttle."},
        new[] {"Integrar ao Explorador do Windows e ao menu de contexto", "Idioma a instalar:", "Pasta de instalação:", "Procurar…", "Opções", "Instalar", "Instalando…", "Instalação concluída", "Executar QBasWing Shuttle", "Fechar", "Integrar ao Explorador do Windows e ao menu de contexto", "Iniciar com o Windows", "Criar atalho na área de trabalho", "Bem-vindo ao instalador do QBasWing Shuttle."},
        new[] {"Integra in Esplora File e nei menu contestuali", "Lingua da installare:", "Cartella di installazione:", "Sfoglia…", "Opzioni", "Installa", "Installazione…", "Installazione completata", "Esegui QBasWing Shuttle", "Chiudi", "Integra in Esplora File e nei menu contestuali", "Avvia con Windows", "Crea collegamento sul desktop", "Benvenuto nell'installatore di QBasWing Shuttle."},
        new[] {"In Windows-Explorer und im Kontextmenü integrieren", "Zu installierende Sprache:", "Installationsordner:", "Durchsuchen…", "Optionen", "Installieren", "Installiere…", "Installation abgeschlossen", "QBasWing Shuttle starten", "Schließen", "In Windows-Explorer und im Kontextmenü integrieren", "Mit Windows starten", "Desktopverknüpfung erstellen", "Willkommen beim QBasWing Shuttle-Installer."},
        new[] {"Integreren in Windows Verkenner en het contextmenu", "Te installeren taal:", "Installatiemap:", "Bladeren…", "Opties", "Installeren", "Bezig met installeren…", "Installatie voltooid", "QBasWing Shuttle uitvoeren", "Sluiten", "Integreren in Windows Verkenner en het contextmenu", "Start met Windows", "Snelkoppeling op bureaublad maken", "Welkom bij de QBasWing Shuttle-installatie."},
        new[] {"Интегрировать в Проводник и контекстное меню", "Язык установки:", "Папка установки:", "Обзор…", "Параметры", "Установить", "Установка…", "Установка завершена", "Запустить QBasWing Shuttle", "Закрыть", "Интегрировать в Проводник и контекстное меню", "Запуск с Windows", "Создать ярлык на рабочем столе", "Добро пожаловать в установщик QBasWing Shuttle."},
        new[] {"Інтегрувати в Провідник і контекстне меню", "Мова для встановлення:", "Папка встановлення:", "Огляд…", "Параметри", "Встановити", "Встановлення…", "Встановлення завершено", "Запустити QBasWing Shuttle", "Закрити", "Інтегрувати в Провідник і контекстне меню", "Запуск із Windows", "Створити ярлик на робочому столі", "Ласкаво просимо до встановлювача QBasWing Shuttle."},
        new[] {"Zintegruj z Eksploratorem plików i menu kontekstowym", "Język instalacji:", "Folder instalacji:", "Przeglądaj…", "Opcje", "Zainstaluj", "Instalowanie…", "Instalacja zakończona", "Uruchom QBasWing Shuttle", "Zamknij", "Zintegruj z Eksploratorem plików i menu kontekstowym", "Uruchom z Windows", "Utwórz skrót na pulpicie", "Witamy w instalatorze QBasWing Shuttle."},
        new[] {"Windows Gezgini ve sağ tık menüsüne tümleştir", "Yüklenecek dil:", "Kurulum klasörü:", "Gözat…", "Seçenekler", "Kur", "Kuruluyor…", "Kurulum tamamlandı", "QBasWing Shuttle'i çalıştır", "Kapat", "Windows Gezgini ve sağ tık menüsüne tümleştir", "Windows ile başlat", "Masaüstü kısayolu oluştur", "QBasWing Shuttle kurulumuna hoş geldiniz."},
        new[] {"Integrovat do Průzkumníka a kontextové nabídky", "Jazyk instalace:", "Instalační složka:", "Procházet…", "Možnosti", "Nainstalovat", "Instaluji…", "Instalace dokončena", "Spustit QBasWing Shuttle", "Zavřít", "Integrovat do Průzkumníka a kontextové nabídky", "Spustit s Windows", "Vytvořit zástupce na ploše", "Vítejte v instalátoru QBasWing Shuttle."},
        new[] {"Instalator QBasWing Shuttle", "Limbă de instalat:", "Folder de instalare:", "Răsfoiește…", "Opțiuni", "Instalează", "Se instalează…", "Instalare finalizată", "Rulează QBasWing Shuttle", "Închide", "Integrare în Explorator de fișiere și meniul contextual", "Pornește cu Windows", "Creează scurtătură pe desktop", "Bine ați venit la instalatorul QBasWing Shuttle."},
        new[] {"QBasWing Shuttle इंस्टॉलर", "स्थापित करने की भाषा:", "स्थापना फ़ोल्डर:", "ब्राउज़ करें…", "विकल्प", "स्थापित करें", "स्थापित हो रहा है…", "स्थापना पूर्ण", "QBasWing Shuttle चलाएँ", "बंद करें", "Windows फ़ाइल एक्सप्लोरर और संदर्भ मेन्यू में एकीकृत करें", "Windows के साथ प्रारंभ", "डेस्कटॉप शॉर्टकट बनाएँ", "QBasWing Shuttle इंस्टॉलर में आपका स्वागत है।"},
        new[] {"دمج في مستكشف Windows وقائمة السياق", "لغة التثبيت:", "مجلد التثبيت:", "استعراض…", "خيارات", "تثبيت", "جارٍ التثبيت…", "اكتمل التثبيت", "تشغيل QBasWing Shuttle", "إغلاق", "دمج في مستكشف Windows وقائمة السياق", "بدء مع Windows", "إنشاء اختصار على سطح المكتب", "مرحباً بك في مثبت QBasWing Shuttle."},
        new[] {"集成到 Windows 资源管理器和右键菜单", "安装语言：", "安装文件夹：", "浏览…", "选项", "安装", "正在安装…", "安装完成", "运行 QBasWing Shuttle", "关闭", "集成到 Windows 资源管理器和右键菜单", "随 Windows 启动", "创建桌面快捷方式", "欢迎使用 QBasWing Shuttle 安装程序。"},
        new[] {"Windows エクスプローラーとコンテキストメニューに統合", "インストールする言語：", "インストール先フォルダー：", "参照…", "オプション", "インストール", "インストール中…", "インストール完了", "QBasWing Shuttle を実行", "閉じる", "Windows エクスプローラーとコンテキストメニューに統合", "Windows とともに起動", "デスクトップにショートカットを作成", "QBasWing Shuttle インストーラーへようこそ。"},
        new[] {"Windows 탐색기 및 컨텍스트 메뉴에 통합", "설치할 언어:", "설치 폴더:", "찾아보기…", "옵션", "설치", "설치 중…", "설치 완료", "QBasWing Shuttle 실행", "닫기", "Windows 탐색기 및 컨텍스트 메뉴에 통합", "Windows와 함께 시작", "바탕 화면 바로 가기 만들기", "QBasWing Shuttle 설치 프로그램에 오신 것을 환영합니다."},
        new[] {"Integrasikan ke Windows Explorer dan menu konteks", "Bahasa yang akan diinstal:", "Folder instalasi:", "Jelajahi…", "Opsi", "Instal", "Menginstal…", "Instalasi selesai", "Jalankan QBasWing Shuttle", "Tutup", "Integrasikan ke Windows Explorer dan menu konteks", "Mulai dengan Windows", "Buat pintasan di desktop", "Selamat datang di penginstal QBasWing Shuttle."},
        new[] {"Tích hợp vào Windows Explorer và menu ngữ cảnh", "Ngôn ngữ cần cài đặt:", "Thư mục cài đặt:", "Duyệt…", "Tùy chọn", "Cài đặt", "Đang cài đặt…", "Cài đặt hoàn tất", "Chạy QBasWing Shuttle", "Đóng", "Tích hợp vào Windows Explorer và menu ngữ cảnh", "Khởi động cùng Windows", "Tạo lối tắt trên màn hình nền", "Chào mừng bạn đến với trình cài đặt QBasWing Shuttle."}
};

    public static int Current = 0;

    public static string Get(int i) => T[Current][i];
}