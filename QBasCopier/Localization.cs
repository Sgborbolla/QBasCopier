using System.Globalization;

namespace QBasCopier;

public static class L
{
    public static readonly string[] Codes = {
        "en","es","pt","fr","de","it","nl","pl","ru","tr","ar",
        "zh-Hans","ja","ko","hi","id","sv","fi","cs","th"
    };

    private static readonly Dictionary<string, string?[]> R = new();

    public static string lang = "es";

    public static int Current
    {
        get { int i = Array.IndexOf(Codes, lang); return i < 0 ? 0 : i; }
    }

    public static string Get(string key)
    {
        if (!R.TryGetValue(key, out var a)) return key;
        int i = Array.IndexOf(Codes, lang);
        if (i < 0) i = 0;
        return a[i] ?? a[0] ?? key;
    }

    /// <summary>
    /// Texto con datos: L.F("trSelDirs", "3", "1"). Los {0}, {1}... se rellenan con
    /// lo que se pase, en el orden del idioma.
    /// </summary>
    public static string F(string key, params object[] args)
    {
        var t = Get(key);
        try { return args.Length == 0 ? t : string.Format(t, args); }
        catch (Exception) { return t; }
    }

    public static void SetLanguage(string l)
    {
        if (Array.IndexOf(Codes, l) >= 0) lang = l;
    }

    public static string[] LanguageNames = {
        "English","Español","Português","Français","Deutsch","Italiano","Nederlands",
        "Polski","Русский","Türkçe","العربية","中文 (简体)","日本語","한국어","हिन्दी",
        "Bahasa Indonesia","Svenska","Suomi","Čeština","ไทย"
    };

    static L()
    {
        // order per Codes[]
        void A(string key, string? en, string? es, string? pt, string? fr, string? de, string? it,
               string? nl, string? pl, string? ru, string? tr, string? ar, string? zh,
               string? ja, string? ko, string? hi, string? id, string? sv, string? fi,
               string? cs, string? th)
        {
            R[key] = new string?[] { en, es, pt, fr, de, it, nl, pl, ru, tr, ar, zh, ja, ko, hi, id, sv, fi, cs, th };
        }

        A("addFiles", "Add files", "Añadir archivos", "Adicionar arquivos", "Ajouter des fichiers", "Dateien hinzufügen",
          "Aggiungi file", "Bestanden toevoegen", "Dodaj pliki", "Добавить файлы", "Dosya ekle",
          "إضافة ملفات", "添加文件", "ファイルを追加", "파일 추가", "फ़ाइलें जोड़ें", "Tambah berkas",
          "Lägg till filer", "Lisää tiedostoja", "Přidat soubory", "เพิ่มไฟล์");
        A("addFolder", "Add folder", "Añadir carpeta", "Adicionar pasta", "Ajouter un dossier", "Ordner hinzufügen",
          "Aggiungi cartella", "Map toevoegen", "Dodaj folder", "Добавить папку", "Klasör ekle",
          "إضافة مجلد", "添加文件夹", "フォルダーを追加", "폴더 추가", "फ़ोल्डर जोड़ें", "Tambah folder",
          "Lägg till mapp", "Lisää kansio", "Přidat složku", "เพิ่มโฟลเดอร์");
        A("destination", "Destination", "Destino", "Destino", "Destination", "Ziel",
          "Destinazione", "Bestemming", "Miejsce docelowe", "Назначение", "Hedef",
          "الوجهة", "目标文件夹", "コピー先", "대상", "गंतव्य", "Tujuan", "Destination",
          "Kohde", "Cíl", "ปลายทาง");
        A("browse", "Browse…", "Examinar…", "Procurar…", "Parcourir…", "Durchsuchen…",
          "Sfoglia…", "Bladeren…", "Przeglądaj…", "Обзор…", "Gözat…",
          "استعراض…", "浏览…", "参照…", "찾아보기…", "ब्राउज़ करें…", "Jelajahi…", "Bläddra…",
          "Selaa…", "Procházet…", "เรียกดู…");
        A("copy", "Copy", "Copiar", "Copiar", "Copier", "Kopieren",
          "Copia", "Kopiëren", "Kopiuj", "Копировать", "Kopyala",
          "نسخ", "复制", "コピー", "복사", "कॉपी", "Salin", "Kopiera", "Kopioi", "Kopírovat", "คัดลอก");
        A("move", "Move", "Mover", "Mover", "Déplacer", "Verschieben",
          "Sposta", "Verplaatsen", "Przenieś", "Переместить", "Taşı",
          "نقل", "移动", "移動", "이동", "स्थानांतरण", "Pindah", "Flytta", "Siirrä", "Přesunout", "ย้าย");
        A("pause", "Pause", "Pausa", "Pausa", "Pause", "Pause",
          "Pausa", "Pauze", "Wstrzymaj", "Пауза", "Duraklat",
          "إيقاف مؤقت", "暂停", "一時停止", "일시정지", "रोकें", "Jeda", "Paus", "Tauko", "Pauza", "หยุดชั่วคราว");
        A("resume", "Resume", "Reanudar", "Retomar", "Reprendre", "Fortsetzen",
          "Riprendi", "Hervatten", "Wznów", "Продолжить", "Sürdür",
          "استئناف", "继续", "再開", "재개", "जारी रखें", "Lanjutkan", "Fortsätt", "Jatka", "Pokračovat", "ดำเนินการต่อ");
        A("cancel", "Cancel", "Cancelar", "Cancelar", "Annuler", "Abbrechen",
          "Annulla", "Annuleren", "Anuluj", "Отменить", "İptal",
          "إلغاء", "取消", "キャンセル", "취소", "रद्द करें", "Batal", "Avbryt", "Peruuta", "Zrušit", "ยกเลิก");
        A("clear", "Clear", "Limpiar", "Limpar", "Effacer", "Leeren",
          "Pulisci", "Wissen", "Wyczyść", "Очистить", "Temizle",
          "مسح", "清空", "クリア", "지우기", "साफ़ करें", "Bersihkan", "Rensa", "Tyhjennä", "Vymazat", "ล้าง");
        A("options", "Options", "Opciones", "Opções", "Options", "Einstellungen",
          "Opzioni", "Opties", "Opcje", "Настройки", "Seçenekler",
          "خيارات", "选项", "オプション", "옵션", "विकल्प", "Opsi", "Alternativ", "Asetukset", "Možnosti", "ตัวเลือก");
        A("tabCopyList", "Copies", "Copiando", "Cópias", "Copies", "Kopien",
          "Copie", "Kopieën", "Kopie", "Копирование", "Kopyalar",
          "النسخ", "复制列表", "コピー", "복사 목록", "प्रतियां", "Salinan", "Kopior", "Kopiot", "Kopie", "รายการคัดลอก");
        A("tabInterface", "Interface", "Interfaz", "Interface", "Interface", "Schnittstelle",
          "Interfaccia", "Interface", "Interfejs", "Интерфейс", "Arayüz",
          "الواجهة", "界面", "画面", "화면", "इंटरफ़ेस", "Antarmuka", "Gränssnitt", "Käyttöliittymä", "Rozhraní", "อินเทอร์เฟซ");
        A("tabHistory", "History", "Historial", "Histórico", "Historique", "Verlauf",
          "Cronologia", "Geschiedenis", "Historia", "История", "Geçmiş",
          "السجل", "历史", "履歴", "기록", "इतिहास", "Riwayat", "Historik", "Historia", "Historie", "ประวัติ");
        A("currentFile", "Current file", "Archivo actual", "Arquivo atual", "Fichier en cours", "Aktuelle Datei",
          "File corrente", "Huidig bestand", "Bieżący plik", "Текущий файл", "Geçerli dosya",
          "الملف الحالي", "当前文件", "現在のファイル", "현재 파일", "वर्तमान फ़ाइल", "Berkas saat ini",
          "Aktuell fil", "Nykyinen tiedosto", "Aktuální soubor", "ไฟล์ปัจจุบัน");
        A("speed", "Speed", "Velocidad", "Velocidade", "Vitesse", "Geschwindigkeit",
          "Velocità", "Snelheid", "Prędkość", "Скорость", "Hız",
          "السرعة", "速度", "速度", "속도", "गति", "Kecepatan", "Hastighet", "Nopeus", "Rychlost", "ความเร็ว");
        A("remaining", "Time left", "Tiempo restante", "Tempo restante", "Temps restant", "Verbleibend",
          "Tempo rimasto", "Resterend", "Pozostało", "Осталось", "Kalan",
          "الوقت المتبقي", "剩余时间", "残り時間", "남은 시간", "शेष समय", "Sisa waktu", "Återstående tid",
          "Jäljellä", "Zbývá", "เวลาที่เหลือ");
        A("elapsed", "Elapsed", "Transcurrido", "Decorrido", "Écoulé", "Vergangen",
          "Trascorso", "Verstreken", "Upłynęło", "Прошло", "Geçen",
          "المنقضي", "已用时间", "経過時間", "경과 시간", "बीता समय", "Berlalu", "Förfluten tid", "Kulunut",
          "Uplynulo", "เวลาที่ใช้");
        A("integrate", "Integrate into system", "Integrar en el sistema", "Integrar ao sistema", "Intégrer au système", "In System integrieren",
          "Integra nel sistema", "Integreren in systeem", "Zintegruj z systemem", "Интегрировать в систему", "Sisteme entegre et",
          "دمج في النظام", "集成到系统", "システムに統合", "시스템에 통합", "सिस्टम में एकीकृत करें", "Integrasikan ke sistem",
          "Integrera i systemet", "Integroi järjestelmään", "Integrovat do systému", "รวมเข้ากับระบบ");
        A("fold", "Collapse", "Plegar", "Recolher", "Replier", "Einklappen",
          "Comprimi", "Inklappen", "Zwiń", "Свернуть", "Daralt",
          "طي", "折叠", "折りたたむ", "접기", "संक्षिप्त करें", "Ciutkan", "Dölj",
          "Tiivistä", "Sbalit", "ย่อ");
        A("interval", "Window refresh interval (ms)", "Intervalo de refresco de ventana (ms)", "Intervalo de atualização da janela (ms)", "Intervalle de rafraîchissement (ms)", "Aktualisierungsintervall (ms)",
          "Intervallo aggiornamento (ms)", "Verversinterval (ms)", "Interwał odświeżania (ms)", "Интервал обновления окна (мс)", "Pencere yenileme aralığı (ms)",
          "فترة تحديث النافذة (مللي ثانية)", "窗口刷新间隔(毫秒)", "画面更新間隔(ms)", "창 새로고침 간격(ms)", "विंडो रीफ़्रेश अंतराल(ms)", "Interval pembaruan jendela (ms)",
          "Uppdateringsintervall (ms)", "Päivitysväli (ms)", "Interval obnovení okna (ms)", "ช่วงเวลารีเฟรชหน้าต่าง (ms)");
        A("busy", "Busy", "Ocupado", "Ocupado", "Occupé", "Beschäftigt",
          "Occupato", "Bezet", "Zajęty", "Занято", "Meşgul",
          "مشغول", "忙", "処理中", "사용 중", "व्यस्त", "Sibuk", "Upptagen",
          "Varattu", "Zaneprázdněno", "ไม่ว่าง");
        A("done", "Done", "Hecho", "Concluído", "Terminé", "Fertig",
          "Fatto", "Klaar", "Gotowe", "Готово", "Tamam",
          "اكتمل", "完成", "完了", "완료", "पूर्ण", "Selesai", "Klar", "Valmis", "Hotovo", "เสร็จสิ้น");
        A("stateReady", "Ready", "Listo", "Pronto", "Prêt", "Bereit",
          "Pronto", "Klaar", "Gotowy", "Готов", "Hazır",
          "جاهز", "就绪", "準備完了", "준비됨", "तैयार", "Siap", "Redo", "Valmis", "Připraven", "พร้อม");
        A("stateCopying", "Copying…", "Copiando…", "Copiando…", "Copie…", "Kopiere…",
          "Copia…", "Bezig…", "Kopiuję…", "Копирование…", "Kopyalanıyor…",
          "جارٍ النسخ…", "正在复制…", "コピー中…", "복사 중…", "कॉपी हो रही…", "Menyalin…", "Kopierar…",
          "Kopioidaan…", "Kopíruje se…", "กำลังคัดลอก…");
        A("stateDone", "Done", "Hecho", "Concluído", "Terminé", "Fertig",
          "Fatto", "Klaar", "Zakończono", "Завершено", "Tamam",
          "اكتمل", "已完成", "完了", "완료", "पूर्ण", "Selesai", "Klar", "Valmis", "Hotovo", "เสร็จ");
        A("stateError", "Error", "Error", "Erro", "Erreur", "Fehler",
          "Errore", "Fout", "Błąd", "Ошибка", "Hata",
          "خطأ", "错误", "エラー", "오류", "त्रुटि", "Kesalahan", "Fel", "Virhe", "Chyba", "ข้อผิดพลาด");
        A("stateSameFile", "Same file", "Es el mismo archivo", "É o mesmo arquivo", "Même fichier", "Gleiche Datei", "Stesso file", "Zelfde bestand", "Ten sam plik",
          "Тот же файл", "Aynı dosya", "نفس الملف", "同一文件", "同じファイル", "같은 파일", "वही फ़ाइल", "Berkas yang sama",
          "Samma fil", "Sama tiedosto", "Stejný soubor", "ไฟล์เดียวกัน");
        A("stateSkipped", "Skipped", "Omitido", "Ignorado", "Ignoré", "Übersprungen",
          "Saltato", "Overgeslagen", "Pominięty", "Пропущено", "Atlandı",
          "تم التجاهل", "已跳过", "スキップ", "건너뜀", "छोड़ा गया", "Dilewati", "Hoppades", "Ohitettu", "Přeskočeno", "ข้าม");
        A("colTitle", "File already exists", "El archivo ya existe", "O arquivo já existe", "Le fichier existe déjà",
          "Die Datei existiert bereits", "Il file esiste già", "Het bestand bestaat al", "Plik już istnieje",
          "Файл уже существует", "Dosya zaten var", "الملف موجود بالفعل", "文件已存在", "ファイルは既に存在します",
          "파일이 이미 있습니다", "फ़ाइल पहले से मौजूद है", "Berkas sudah ada", "Filen finns redan",
          "Tiedosto on jo olemassa", "Soubor již existuje", "ไฟล์มีอยู่แล้ว");
        A("skip", "Skip", "Omitir", "Ignorar", "Ignorer", "Überspringen",
          "Salta", "Overslaan", "Pomiń", "Пропустить", "Atla",
          "تجاهل", "跳过", "スキップ", "건너뛰기", "छोड़ें", "Lewati", "Hoppa", "Ohita", "Přeskočit", "ข้าม");
        A("overwrite", "Overwrite", "Sobrescribir", "Sobrescrever", "Écraser", "Überschreiben",
          "Sovrascrivi", "Overschrijven", "Nadpisz", "Перезаписать", "Üzerine yaz",
          "استبدال", "覆盖", "上書き", "덮어쓰기", "अधिलेखित", "Timpa", "Skriv över", "Korvaa", "Přepsat", "เขียนทับ");
        A("overwriteDiff", "If different", "Si es diferente", "Se for diferente", "Si différent", "Wenn anders",
          "Se diverso", "Indien anders", "Jeżeli inny", "Если отличается", "Farklıysa",
          "إذا كان مختلفًا", "如不同", "異なるなら", "다르면", "भिन्न होने पर", "Jika berbeda", "Om annorlunda",
          "Jos eroaa", "Pokud se liší", "ถ้าต่างกัน");
        A("rename", "Rename", "Renombrar", "Renomear", "Renommer", "Umbenennen",
          "Rinomina", "Hernoemen", "Zmień nazwę", "Переименовать", "Yeniden adlandır",
          "إعادة تسمية", "重命名", "名前を変更", "이름 바꾸기", "नाम बदलें", "Ganti nama", "Byt namn",
          "Nimeä uudelleen", "Přejmenovat", "เปลี่ยนชื่อ");
        A("retry", "Retry", "Reintentar", "Tentar novamente", "Réessayer", "Wiederholen",
          "Riprova", "Opnieuw", "Ponów", "Повторить", "Yeniden dene",
          "إعادة المحاولة", "重试", "再試行", "재시도", "पुनः प्रयास", "Ulangi", "Försök igen", "Yritä uudelleen",
          "Zkusit znovu", "ลองอีกครั้ง");
        A("allfiles", "All", "Todo", "Todos", "Tous", "Alle",
          "Tutto", "Alle", "Wszystko", "Все", "Tümü",
          "الكل", "全部", "すべて", "모두", "सभी", "Semua", "Alla", "Kaikki", "Vše", "ทั้งหมด");
        A("startWithWindows", "Start with Windows", "Iniciar con Windows", "Iniciar com o Windows", "Démarrer avec Windows",
          "Mit Windows starten", "Avvia con Windows", "Start met Windows", "Uruchamiaj z systemem",
          "Запускать с Windows", "Windows ile başlat", "البدء مع Windows", "随 Windows 启动", "Windows とともに起動",
          "Windows와 함께 시작", "विंडोज़ के साथ प्रारंभ", "Mulai dengan Windows", "Starta med Windows",
          "Käynnistä Windowsin kanssa", "Spustit s Windowsem", "เริ่มต้นพร้อม Windows");
        A("minToTray", "Minimize to tray", "Minimizar a bandeja", "Minimizar para a bandeja", "Réduire dans la zone de notification",
          "In den Infobereich minimieren", "Riduci nell'area di notifica", "Minimaliseren naar systeemvak",
          "Minimalizuj do zasobnika", "Сворачивать в трей", "Tepsime küçült", "تصغير إلى الدرج", "最小化到托盘",
          "トレイに最小化", "트레이로 최소화", "ट्रे में छोटा करें", "Minimalkan ke baki", "Minimera till fack",
          "Pienennä ilmaisinalueelle", "Minimalizovat do lišty", "ย่อไปถาด");
        A("quit", "Quit", "Salir", "Sair", "Quitter", "Beenden",
          "Esci", "Afsluiten", "Zakończ", "Выход", "Çıkış",
          "خروج", "退出", "終了", "종료", "बाहर जाएं", "Keluar", "Avsluta", "Lopeta", "Ukončit", "ออก");
        A("open", "Open", "Abrir", "Abrir", "Ouvrir", "Öffnen",
          "Apri", "Openen", "Otwórz", "Открыть", "Aç",
          "فتح", "打开", "開く", "열기", "खोलें", "Buka", "Öppna", "Avaa", "Otevřít", "เปิด");
        A("histEmpty", "No history yet", "Sin historial todavía", "Sem histórico ainda", "Aucun historique",
          "Noch kein Verlauf", "Nessuna cronologia", "Nog geen geschiedenis", "Brak historii",
          "История пуста", "Henüz geçmiş yok", "لا يوجد سجل بعد", "暂无历史", "履歴はまだありません",
          "기록이 아직 없음", "अभी कोई इतिहास नहीं", "Belum ada riwayat", "Ingen historik ännu",
          "Ei historiaa vielä", "Zatím žádná historie", "ยังไม่มีประวัติ");
        A("histClear", "Clear history", "Borrar historial", "Limpar histórico", "Effacer l'historique", "Verlauf löschen",
          "Cancella cronologia", "Geschiedenis wissen", "Wyczyść historię", "Очистить историю", "Geçmişi temizle",
          "مسح السجل", "清空历史", "履歴を消去", "기록 지우기", "इतिहास साफ़ करें", "Bersihkan riwayat",
          "Rensa historik", "Tyhjennä historia", "Vymazat historii", "ล้างประวัติ");
        A("histOpen", "Open location", "Abrir ubicación", "Abrir local", "Ouvrir l'emplacement", "Speicherort öffnen",
          "Apri percorso", "Locatie openen", "Otwórz lokalizację", "Открыть папку", "Konumu aç",
          "فتح الموقع", "打开位置", "場所を開く", "위치 열기", "स्थान खोलें", "Buka lokasi", "Öppna plats",
          "Avaa sijainti", "Otevřít umístění", "เปิดตำแหน่ง");
        A("install", "Install integration", "Instalar integración", "Instalar integração", "Installer l'intégration",
          "Integration installieren", "Installa integrazione", "Integratie installeren", "Zainstaluj integrację",
          "Установить интеграцию", "Entegrasyonu kur", "تثبيت التكامل", "安装集成", "連携をインストール",
          "통합 설치", "एकीकरण स्थापित करें", "Pasang integrasi", "Installera integration", "Asenna integraatio",
          "Nainstalovat integraci", "ติดตั้งการผสาน");
        A("sLanguage", "Language", "Idioma", "Idioma", "Langue", "Sprache",
          "Lingua", "Taal", "Język", "Язык", "Dil",
          "اللغة", "语言", "言語", "언어", "भाषा", "Bahasa", "Språk", "Kieli", "Jazyk", "ภาษา");
        A("sDefaults", "Copies & moves defaults", "Valores predeterminados", "Padrões de cópia/movimentação",
          "Copies et déplacements par défaut", "Standard für Kopien/Verschieben", "Predefiniti per copie/spostamenti",
          "Standaard kopiëren/verplaatsen", "Domyślne dla kopii/przenoszenia", "Параметры по умолчанию",
          "Kopyalama/taşıma varsayılanları", "إعدادات النسخ والنقل الافتراضية", "复制/移动默认设置",
          "コピー/移動の既定", "복사/이동 기본값", "कॉपी/मूव डिफ़ॉल्ट", "Default salin/pindah",
          "Standardinställningar", "Kopioinnin/siirron oletukset", "Výchozí kopie/přesun",
          "ค่าเริ่มต้นการคัดลอก/ย้าย");
        A("sLog", "Error log", "Registro de errores", "Registro de erros", "Journal des erreurs", "Fehlerprotokoll",
          "Registro errori", "Foutenlogboek", "Dziennik błędów", "Журнал ошибок", "Hata günlüğü",
          "سجل الأخطاء", "错误日志", "エラーログ", "오류 로그", "त्रुटि लॉग", "Log kesalahan", "Fel logg",
          "Virheloki", "Protokol chyb", "บันทึกข้อผิดพลาด");
        A("ok", "OK", "Aceptar", "OK", "OK", "OK",
          "OK", "OK", "OK", "ОК", "Tamam",
          "حسناً", "确定", "OK", "확인", "ठीक", "OK", "OK", "OK", "OK", "ตกลง");
        A("apply", "Apply", "Aplicar", "Aplicar", "Appliquer", "Übernehmen",
          "Applica", "Toepassen", "Zastosuj", "Применить", "Uygula",
          "تطبيق", "应用", "適用", "적용", "लागू", "Terapkan", "Tillämpa", "Käytä", "Použít", "นำไปใช้");
        
        // ---- Engine / Interface / Options pages ----
        A("engineType", "Copy engine", "Motor de copia", "Motor de cópia", "Moteur de copie", "Kopiermotor",
          "Motore di copia", "Kopijmotor", "Silnik kopiowania", "Механизм копирования", "Kopyalama motoru",
          "محرك النسخ", "复制引擎", "コピーエンジン", "복사 엔진", "कॉपी इंजन", "Mesin salin",
          "Kopieringsmotor", "Kopiointimoottori", "Kopírovací jádro", "เอนจินคัดลอก");
        A("native", "Native (CopyFileEx)", "Nativo (CopyFileEx)", "Nativo (CopyFileEx)", "Natif (CopyFileEx)",
          "Nativ (CopyFileEx)", "Nativo (CopyFileEx)", "Native (CopyFileEx)", "Natywny (CopyFileEx)",
          "Нативный (CopyFileEx)", "Yerel (CopyFileEx)", "أصلي (CopyFileEx)", "原生 (CopyFileEx)",
          "ネイティブ (CopyFileEx)", "네이티브 (CopyFileEx)", "नेटिव (CopyFileEx)", "Asli (CopyFileEx)",
          "Intern (CopyFileEx)", "Natiivi (CopyFileEx)", "Nativní (CopyFileEx)", "ดั้งเดิม (CopyFileEx)");
        A("buffered", "Buffered", "Búfer", "Buffer", "Tampon", "Gepuffert",
          "Bufferizzato", "Gebufferd", "Buforowany", "Буферизированный", "Tamponlu",
          "مخزّن", "缓冲", "バッファ", "버퍼", "बफ़र्ड", "Terutamper",
          "Buffrad", "Puskuroitu", "Vyrovnávací paměť", "บัฟเฟอร์");
        A("threads", "Parallel copies", "Copias paralelas", "Cópias paralelas", "Copies parallèles",
          "Parallele Kopien", "Copie parallele", "Parallele kopieën", "Kopie równoległe",
          "Параллельные копии", "Paralel kopyalamalar", "نسخ متوازٍ", "并行复制",
          "並列コピー", "병렬 복사", "समानांतर प्रतियाँ", "Salinan paralel",
          "Parallella kopior", "Rinnakkaiset kopiot", "Paralelní kopie", "การคัดลอกแบบขนาน");
        A("buffer", "Buffer size", "Tamaño de búfer", "Tamanho do buffer", "Taille du tampon", "Puffergröße",
          "Dimensione buffer", "Buffergrootte", "Rozmiar bufora", "Размер буфера", "Tampon boyutu",
          "حجم المخزن المؤقت", "缓冲区大小", "バッファサイズ", "버퍼 크기", "बफ़र आकार", "Ukuran buffer",
          "Buffertstorlek", "Puskurin koko", "Velikost vyrovnávací paměti", "ขนาดบัฟเฟอร์");
        A("speedLimit", "Speed limit", "Límite de velocidad", "Limite de velocidade", "Limite de vitesse",
          "Geschwindigkeitsbegrenzung", "Limite di velocità", "Snelheidslimiet", "Limit prędkości",
          "Ограничение скорости", "Hız sınırı", "حد السرعة", "速度限制",
          "速度制限", "속도 제한", "गति सीमा", "Batas kecepatan",
          "Hastighetsgräns", "Nopeusrajoitus", "Rychlostní limit", "ขีดจำกัดความเร็ว");
        A("auto", "Auto", "Automático", "Automático", "Auto", "Automatisch",
          "Auto", "Automatisch", "Automatycznie", "Авто", "Otomatik",
          "تلقائي", "自动", "自動", "자동", "स्वतः", "Otomatis",
          "Automatisk", "Automaattinen", "Automaticky", "อัตโนมัติ");
        A("units", "Size unit", "Unidad de tamaño", "Unidade de tamanho", "Unité de taille", "Größenangabe",
          "Unità di misura", "Maateenheid", "Jednostka rozmiaru", "Единица размера", "Boyut birimi",
          "وحدة الحجم", "大小单位", "容量単位", "크기 단위", "आकार इकाई", "Satuan ukuran",
          "Storleksenhet", "Kokoyksikkö", "Jednotka velikosti", "หน่วยขนาด");
        A("showInTitle", "Progress in window title", "Progreso en el título", "Progresso no título",
          "Progression dans le titre", "Fortschritt im Titel", "Avanzamento nel titolo", "Voortgang in titel",
          "Postęp w tytule", "Прогресс в заголовке", "Başlıkta ilerleme", "التقدم في العنوان",
          "标题中显示进度", "タイトルに進捗", "제목에 진행 표시", "शीर्षक में प्रगति", "Kemajuan di judul",
          "Förlopp i titeln", "Edistyminen otsikossa", "Průběh v nadpisu", "ความคืบหน้าในชื่อเรื่อง");
        A("errTitle", "Copy failed", "Error al copiar", "Falha ao copiar", "Échec de la copie",
          "Kopieren fehlgeschlagen", "Errore di copia", "Fout bij kopiëren", "Błąd kopiowania",
          "Ошибка копирования", "Kopyalama hatası", "فشل النسخ", "复制失败",
          "コピーに失敗", "복사 실패", "कॉपी विफल", "Gagal menyalin",
          "Kopiering misslyckades", "Kopiointi epäonnistui", "Kopírování se nezdařilo", "การคัดลอกล้มเหลว");
        A("errValue", "Error details", "Detalles del error", "Detalhes do erro", "Détails de l'erreur",
          "Fehlerdetails", "Dettagli errore", "Foutdetails", "Szczegóły błędu", "Подробности ошибки",
          "Hata ayrıntıları", "تفاصيل الخطأ", "错误详情", "エラー詳細", "오류 세부정보", "त्रुटि विवरण",
          "Detail kesalahan", "Felinformation", "Virhetiedot", "Detaily chyby", "รายละเอียดข้อผิดพลาด");
        // ---- Settings pages ----
        A("retryInterval", "Retry interval", "Intervalo de reintento", "Intervalo de repetição",
          "Intervalle de réessai", "Wiederholungsintervall", "Intervallo nuovo tentativo",
          "Interval opnieuw", "Opóźnienie ponowienia", "Интервал повтора", "Tekrar deneme aralığı",
          "فترة إعادة المحاولة", "重试间隔", "再試行間隔", "재시도 간격", "पुनः प्रयास अंतराल",
          "Interval pengulangan", "Försöksintervall", "Uudelleenyrityksen väli",
          "Interval opakování", "ช่วงลองใหม่");
        A("afterDone", "After copy", "Al terminar", "Ao terminar", "À la fin", "Nach der Kopie",
          "Dopo la copia", "Na de kopie", "Po zakończeniu", "По завершении", "Kopyadan sonra",
          "بعد النسخ", "复制完成后", "コピー後", "복사 후", "कॉपी के बाद", "Setelah salin",
          "Efter kopiering", "Kopioinnin jälkeen", "Po dokončení", "หลังคัดลอก");
        A("afterClose", "Close window", "Cerrar ventana", "Fechar janela", "Fermer la fenêtre",
          "Fenster schließen", "Chiudi finestra", "Venster sluiten", "Zamknij okno",
          "Закрыть окно", "Pencereyi kapat", "إغلاق النافذة", "关闭窗口",
          "ウィンドウを閉じる", "창 닫기", "विंडो बंद करें", "Tutup jendela",
          "Stäng fönstret", "Sulje ikkuna", "Zavřít okno", "ปิดหน้าต่าง");
        A("afterKeep", "Keep open", "Dejar abierta", "Manter aberta", "Laisser ouverte", "Offen lassen",
          "Lascia aperta", "Open laten", "Zostaw otwarte", "Оставить открытой", "Açık bırak",
          "إبقاء مفتوحة", "保持打开", "開いたまま", "열어 두기", "खुला रखें", "Biarkan terbuka",
          "Lämna öppen", "Jätä auki", "Nechat otevřené", "เปิดค้างไว้");
        A("afterKeepErr", "Keep open if errors", "Dejar abierta si hay errores",
          "Manter aberta se houver erros", "Laisser ouverte en cas d'erreur", "Bei Fehlern offen lassen",
          "Lascia aperta se errori", "Open bij fouten", "Zostaw otwarte przy błędach",
          "Оставлять при ошибках", "Hata varsa açık bırak", "إبقاء مفتوحة عند الأخطاء",
          "有错误时保持打开", "エラー時は開いたまま", "오류 시 열어 두기", "त्रुटियों पर खुला रखें",
          "Biarkan terbuka jika salah", "Lämna öppen vid fel", "Jätä auki virheiden sattuessa",
          "Při chybách nechat otevřené", "เปิดค้างไว้เมื่อมีข้อผิดพลาด");
        A("collisionDefault", "Default on existing file", "Ante archivo existente", "Se arquivo existir",
          "En cas de fichier existant", "Standard bei vorhandener Datei", "Default se file esiste",
          "Standaard bij bestaand bestand", "Domyślnie przy istniejącym pliku", "При существующем файле",
          "Zaten var olan dosyada", "عند وجود ملف", "目标存在时的默认动作",
          "既存ファイル時の既定", "기존 파일 시 기본", "मौजूदा फ़ाइल पर डिफ़ॉल्ट",
          "Default jika berkas ada", "Standard om fil finns", "Oletus kun tiedosto on olemassa",
          "Výchozí při existujícím souboru", "ค่าเริ่มต้นเมื่อไฟล์มีอยู่");
        A("colAsk", "Ask", "Preguntar", "Perguntar", "Demander", "Fragen",
          "Chiedi", "Vragen", "Pytaj", "Спрашивать", "Sor",
          "اسأل", "询问", "確認する", "질문", "पूछें", "Tanya",
          "Fråga", "Kysy", "Poptat", "ถาม");
        A("colCancelAll", "Cancel all", "Cancelar todo", "Cancelar tudo", "Tout annuler", "Alles abbrechen",
          "Annulla tutto", "Alles annuleren", "Anuluj wszystko", "Отменить всё", "Hepsini iptal et",
          "إلغاء الكل", "全部取消", "すべてキャンセル", "모두 취소", "सब रद्द करें", "Batal semua",
          "Avbryt allt", "Peruuta kaikki", "Zrušit vše", "ยกเลิกทั้งหมด");
        A("errAsk", "Ask", "Preguntar", "Perguntar", "Demander", "Fragen",
          "Chiedi", "Vragen", "Pytaj", "Спрашивать", "Sor",
          "اسأل", "询问", "確認する", "질문", "पूछें", "Tanya",
          "Fråga", "Kysy", "Poptat", "ถาม");
        A("errCancelD", "Cancel copy", "Cancelar copia", "Cancelar cópia", "Annuler la copie",
          "Kopie abbrechen", "Annulla copia", "Kopie annuleren", "Anuluj kopię",
          "Отменить копирование", "Kopyalamayı iptal et", "إلغاء النسخ", "取消复制",
          "コピーをキャンセル", "복사 취소", "कॉपी रद्द करें", "Batalkan salin",
          "Avbryt kopiering", "Peruuta kopiointi", "Zrušit kopírování", "ยกเลิกการคัดลอก");
        A("addListsWhen", "Add new copy lists when", "Añadir nuevas listas cuando",
          "Adicionar novas listas quando", "Ajouter de nouvelles listes quand",
          "Neue Kopierlisten hinzufügen, wenn", "Aggiungi nuove liste quando",
          "Nieuwe kopielijsten toevoegen wanneer", "Dodaj nowe listy kopiowania, gdy",
          "Добавлять новые списки, когда", "Yeni listeler ekle ne zaman", "إضافة قوائم جديدة عند",
          "何时添加新复制列表", "新しいリストを追加する条件", "새 목록 추가 기준",
          "नई प्रतियाँ कब जोड़ें", "Tambah daftar baru saat", "Lägg till nya kopior när",
          "Lisää uusi kopio kun", "Přidat nové seznamy, když", "เพิ่มรายการใหม่เมื่อ");
        A("lstNever", "Never", "Nunca", "Nunca", "Jamais", "Nie",
          "Mai", "Nooit", "Nigdy", "Никогда", "Asla",
          "أبداً", "从不", "しない", "절대 안 함", "कभी नहीं", "Tidak pernah",
          "Aldrig", "Ei koskaan", "Nikdy", "ไม่เคย");
        A("lstAlways", "Always", "Siempre", "Sempre", "Toujours", "Immer",
          "Sempre", "Altijd", "Zawsze", "Всегда", "Her zaman",
          "دائماً", "始终", "常に", "항상", "हमेशा", "Selalu",
          "Alltid", "Aina", "Vždy", "เสมอ");
        A("lstSameSource", "Same source", "Misma fuente", "Mesma origem", "Même source", "Gleiche Quelle",
          "Stessa origine", "Zelfde bron", "To samo źródło", "Тот же источник", "Aynı kaynak",
          "نفس المصدر", "相同来源", "同じ元", "동일한 원본", "समान स्रोत", "Sumber sama",
          "Samma källa", "Sama lähde", "Stejný zdroj", "แหล่งเดียวกัน");
        A("lstSameDest", "Same destination", "Mismo destino", "Mesmo destino", "Même destination",
          "Gleiches Ziel", "Stessa destinazione", "Zelfde bestemming", "To samo miejsce",
          "То же назначение", "Aynı hedef", "نفس الوجهة", "相同目标",
          "同じ先", "동일한 대상", "समान गंतव्य", "Tujuan sama",
          "Samma destination", "Sama kohde", "Stejný cíl", "ปลายทางเดียวกัน");
        A("lstBoth", "Both same", "Ambos iguales", "Ambos iguais", "Mêmes deux", "Beide gleich",
          "Entrambi uguali", "Beide gelijk", "Oba takie same", "Оба совпадают", "İkisi de aynı",
          "كلاهما متماثل", "两者相同", "両方同じ", "둘 다 같음", "दोनों समान", "Keduanya sama",
          "Båda samma", "Molemmat samat", "Obě stejné", "ทั้งคู่เหมือนกัน");
        A("lstEither", "Same source or destination", "Misma fuente o destino", "Mesma origem ou destino",
          "Même source ou destination", "Gleiche Quelle oder Ziel", "Stessa origine o destinazione",
          "Zelfde bron of bestemming", "Ta sama źródło lub cel", "Оба или любой",
          "Aynı kaynak veya hedef", "نفس المصدر أو الوجهة", "相同来源或目标",
          "元か先が同じ", "원본 또는 대상 동일", "समान स्रोत या गंतव्य", "Sumber atau tujuan sama",
          "Samma källa eller destination", "Sama lähde tai kohde", "Stejný zdroj nebo cíl",
          "ต้นทางหรือปลายทางเดียวกัน");
        A("askConfirm", "Confirm before adding", "Confirmar al añadir", "Confirmar ao adicionar",
          "Confirmer avant d'ajouter", "Vor dem Hinzufügen bestätigen", "Conferma prima di aggiungere",
          "Bevestigen voor toevoegen", "Potwierdź przed dodaniem", "Подтверждать добавление",
          "Eklemeden önce onayla", "تأكيد قبل الإضافة", "添加前确认",
          "追加前に確認", "추가 전 확인", "जोड़ने से पहले पुष्टि", "Konfirmasi sebelum menambah",
          "Bekräfta före tillägg", "Vahvista ennen lisäystä", "Potvrdit před přidáním", "ยืนยันก่อนเพิ่ม");
        A("copyAttribs", "Copy attributes", "Copiar atributos", "Copiar atributos", "Copier les attributs",
          "Attribute kopieren", "Copia attributi", "Kenmerken kopiëren", "Kopiuj atrybuty",
          "Копировать атрибуты", "Öznitelikleri kopyala", "نسخ السمات", "复制属性",
          "属性をコピー", "특성 복사", "गुण कॉपी करें", "Salin atribut",
          "Kopiera attribut", "Kopioi ominaisuudet", "Kopírovat atributy", "คัดลอกคุณสมบัติ");
        A("copySecurity", "Copy security (ACL)", "Copiar permisos (ACL)", "Copiar permissões (ACL)",
          "Copier les permissions (ACL)", "Sicherheit kopieren (ACL)", "Copia sicurezza (ACL)",
          "Beveiliging kopiëren (ACL)", "Kopiuj uprawnienia (ACL)", "Копировать права (ACL)",
          "Güvenliği kopyala (ACL)", "نسخ الأمان (ACL)", "复制安全设置 (ACL)",
          "セキュリティをコピー (ACL)", "보안 복사 (ACL)", "सुरक्षा कॉपी (ACL)",
          "Salin keamanan (ACL)", "Kopiera säkerhet (ACL)", "Kopioi turvallisuus (ACL)",
          "Kopírovat zabezpečení (ACL)", "คัดลอกความปลอดภัย (ACL)");
        A("delUnfinished", "Delete unfinished copies", "Borrar copias incompletas", "Apagar cópias incompletas",
          "Supprimer les copies incomplètes", "Unvollständige Kopien löschen", "Elimina copie incomplete",
          "Onvolledige kopieën verwijderen", "Usuwaj nieukończone kopie", "Удалять незавершённые",
          "Tamamlanmamış kopyaları sil", "حذف النسخ غير المكتملة", "删除未完成复制",
          "未完了のコピーを削除", "미완료 복사 삭제", "अधूरी प्रतियाँ हटाएँ", "Hapus salinan belum selesai",
          "Ta bort ofullständiga kopior", "Poista keskeneräiset kopiot", "Smazat nedokončené kopie",
          "ลบการคัดลอกที่ไม่สมบูรณ์");
        A("keepOnError", "Keep incomplete if error", "Conservar si hubo error", "Manter se houver erro",
          "Garder en cas d'erreur", "Bei Fehler behalten", "Conserva se errori",
          "Bewaren bij fout", "Zachowaj przy błędzie", "Оставлять при ошибке",
          "Hata varsa koru", "الاحتفاظ عند الخطأ", "出错时保留",
          "エラー時は保持", "오류 시 유지", "त्रुटि पर रखें", "Pertahankan jika salah",
          "Behåll vid fel", "Pidä virheen sattuessa", "Zachovat při chybě", "เก็บไว้เมื่อมีข้อผิดพลาด");
        A("priority", "Process priority", "Prioridad del proceso", "Prioridade do processo",
          "Priorité du processus", "Prozesspriorität", "Priorità processo", "Procesprioriteit",
          "Priorytet procesu", "Приоритет процесса", "İşlem önceliği", "أولوية العملية",
          "进程优先级", "プロセスの優先度", "프로세스 우선순위", "प्रक्रिया प्राथमिकता",
          "Prioritas proses", "Processprioritet", "Prosessin prioriteetti", "Priorita procesu",
          "ลำดับความสำคัญของกระบวนการ");
        A("speedAvg", "Speed average", "Promedio de velocidad", "Média de velocidade",
          "Moyenne des vitesses", "Durchschnittsgeschwindigkeit", "Media velocità",
          "Gemiddelde snelheid", "Średnia prędkość", "Средняя скорость", "Ortalama hız",
          "متوسط السرعة", "平均速度", "平均速度", "평균 속도", "औसत गति",
          "Rata-rata kecepatan", "Medelhastighet", "Keskinopeus", "Průměrná rychlost", "ความเร็วเฉลี่ย");
        A("throttle", "Throttle interval", "Intervalo de control", "Intervalo de limite",
          "Intervalle de freinage", "Drosselungsintervall", "Intervallo limitazione",
          "Beperkingsinterval", "Interval regulacji", "Интервал контроля", "Sınır aralığı",
          "فترة التحديد", "节流间隔", "制御間隔", "제한 주기", "नियंत्रण अंतराल",
          "Interval pembatas", "Strypningsintervall", "Rajoitusväli", "Interval omezení", "ช่วงจำกัด");
        A("overwriteRO", "Overwrite read-only files", "Sobrescribir solo lectura",
          "Sobrescrever só leitura", "Écraser les fichiers en lecture seule",
          "Schreibgeschützte überschreiben", "Sovrascrivi sola lettura", "Alleen-lezen overschrijven",
          "Nadpisuj tylko do odczytu", "Перезаписывать только для чтения", "Salt okunur üzerine yaz",
          "استبدال الملفات للقراءة فقط", "覆盖只读文件", "読み取り専用を上書き",
          "읽기 전용 덮어쓰기", "रीड-ओनली अधिलेखित", "Timpa hanya-baca",
          "Skriv över skrivskyddade", "Korvaa vain luku", "Přepsat jen pro čtení", "เขียนทับไฟล์อ่านอย่างเดียว");
        A("skipHidden", "Skip hidden/system files", "Omitir ocultos/sistema", "Ignorar ocultos/sistema",
          "Ignorer fichiers masqués/système", "Versteckte/System überspringen", "Salta nascosti/sistema",
          "Verborgen/systeem overslaan", "Pomijaj ukryte/systemowe", "Пропускать скрытые/системные",
          "Gizli/sistem atla", "تجاهل المخفي/النظام", "跳过隐藏/系统文件",
          "隠し/システムをスキップ", "숨김/시스템 건너뜀", "छिपी/सिस्टम छोड़ें",
          "Lewati tersembunyi/sistem", "Hoppa över dolda/system", "Ohita piilotetut/järjestelmä",
          "Přeskočit skryté/systémové", "ข้ามไฟล์ซ่อนเร้น/ระบบ");
        // ---- History columns ----
        A("histFrom", "From", "De", "De", "Depuis", "Von",
          "Da", "Van", "Z", "Из", "Nereden",
          "من", "来源", "元", "원본", "से", "Dari",
          "Från", "Mistä", "Od", "จาก");
        A("histTo", "To", "Hacia", "Para", "Vers", "Nach",
          "A", "Naar", "Do", "В", "Nereye",
          "إلى", "目标", "先", "대상", "को", "Ke",
          "Till", "Kohde", "Do", "ไปยัง");
        A("histOk", "Done", "Hecho", "Concluído", "Terminé", "Fertig",
          "Fatto", "Klaar", "Gotowe", "Готово", "Tamam",
          "اكتمل", "完成", "完了", "완료", "पूर्ण", "Selesai",
          "Klar", "Valmis", "Hotovo", "เสร็จ");
        A("histErrors", "Errors", "Errores", "Erros", "Erreurs", "Fehler",
          "Errori", "Fouten", "Błędy", "Ошибки", "Hatalar",
          "الأخطاء", "错误", "エラー", "오류", "त्रुटियाँ", "Kesalahan",
          "Fel", "Virheet", "Chyby", "ข้อผิดพลาด");
        A("histCancelled", "Cancelled", "Cancelado", "Cancelado", "Annulé", "Abgebrochen",
          "Annullato", "Geannuleerd", "Anulowano", "Отменено", "İptal edildi",
          "ملغي", "已取消", "キャンセル済み", "취소됨", "रद्द", "Dibatalkan",
          "Avbruten", "Peruttu", "Zrušeno", "ถูกยกเลิก");
        A("version", "Version", "Versión", "Versão", "Version", "Version",
          "Versione", "Versie", "Wersja", "Версия", "Sürüm",
          "الإصدار", "版本", "バージョン", "버전", "संस्करण", "Versi",
          "Version", "Versio", "Verze", "เวอร์ชัน");
        A("madeBy", "Made by", "Hecho por", "Feito por", "Créé par", "Erstellt von",
          "Fatto da", "Gemaakt door", "Autor:", "Сделал", "Yapan",
          "صنع بواسطة", "制作", "作成者", "제작자", "निर्माता", "Dibuat oleh",
          "Skapad av", "Tekijä", "Autor", "สร้างโดย");
        A("engineInfo", "Our parallel copy engine — very fast",
          "Nuestro motor de copia en paralelo: rapidísimo",
          "O nosso motor de cópia em paralelo: rapidíssimo",
          "Notre moteur de copie en parallèle : très rapide",
          "Eigene parallele Kopier-Engine – blitzschnell",
          "Il nostro motore di copia in parallelo: velocissimo",
          "Eigen parallelle kopieermotor – razendsnel",
          "Nasz silnik równoległego kopiowania – błyskawiczny",
          "Наш параллельный движок копирования — очень быстрый",
          "Kendi paralel kopyalama motorumuz — çok hızlı",
          "محرك النسخ المتوازي الخاص بنا — سريع للغاية",
          "我们的并行复制引擎 — 闪电般快速",
          "自社並列コピーエンジン — 超高速",
          "자사 병렬 복사 엔진 — 초고속",
          "हमारा समानांतर कॉपी इंजन — बेहद तेज़",
          "Mesin salin paralel kami — sangat cepat",
          "Vår parallella kopieringsmotor – blixtsnabb",
          "Oma rinnakkainen kopiointimoottorimme – salamannopea",
          "Náš paralelní jádro kopírování — bleskurychlé",
          "เอนจินคัดลอกแบบขนานของเรา — เร็วสุด ๆ");
        A("rights", "All rights reserved", "Todos los derechos reservados", "Todos os direitos reservados",
          "Tous droits réservés", "Alle Rechte vorbehalten", "Tutti i diritti riservati",
          "Alle rechten voorbehouden", "Wszelkie prawa zastrzeżone", "Все права защищены",
          "Tüm hakları saklıdır", "جميع الحقوق محفوظة", "版权所有",
          "無断転載禁止", "판권 소유", "सर्वाधिकार सुरक्षित", "Hak cipta dilindungi",
          "Med ensamrätt", "Kaikki oikeudet pidätetään", "Všechna práva vyhrazena", "สงวนลิขสิทธิ์");

        // ---- Interfaz nueva: barra de menus, barra de comandos y barra individual ----
        A("menuFile", "File", "Archivo", "Arquivo", "Fichier", "Datei", "File",
          "Bestand", "Plik", "Файл", "Dosya", "ملف", "文件",
          "ファイル", "파일", "फ़ाइल", "Berkas", "Fil", "Tiedosto", "Soubor", "ไฟล์");
        A("menuEdit", "Edit", "Edición", "Edição", "Édition", "Bearbeiten", "Modifica",
          "Bewerken", "Edytuj", "Редактировать", "Düzenle", "تحرير", "编辑",
          "編集", "편집", "संपादित करें", "Sunting", "Redigera", "Muokkaa", "Upravit", "แก้ไข");
        A("menuView", "View", "Ver", "Ver", "Affichage", "Ansicht", "Visualizza",
          "Weergave", "Widok", "Вид", "Görünüm", "عرض", "视图",
          "表示", "보기", "दृश्य", "Tampilan", "Vy", "Näkymä", "Pohled", "มุมมอง");
        A("menuTools", "Tools", "Herramientas", "Ferramentas", "Outils", "Werkzeuge", "Strumenti",
          "Extra's", "Narzędzia", "Инструменты", "Araçlar", "أدوات", "工具",
          "ツール", "도구", "उपकरण", "Alat", "Verktyg", "Työkalut", "Nástroje", "เครื่องมือ");
        A("menuHelp", "Help", "Ayuda", "Ajuda", "Aide", "Hilfe", "Aiuto",
          "Help", "Pomoc", "Справка", "Yardım", "مساعدة", "帮助",
          "ヘルプ", "도움말", "मदद", "Bantuan", "Hjälp", "Ohje", "Nápověda", "ช่วยเหลือ");

        A("up", "Up", "Subir", "Subir", "Remonter", "Hoch", "Su", "Omhoog", "W górę",
          "Вверх", "Fel", "إلى أعلى", "上", "上へ", "위로", "ऊपर", "Na atas",
          "Upp", "Ylös", "Nahoru", "ขึ้น");
        A("nothingHere", "Nothing here yet", "Aquí no hay nada aún", "Aqui não há nada ainda", "Rien ici pour l’instant", "Hier ist noch nichts", "Qui non c’è ancora niente", "Hier is nog niets", "Tu jeszcze nic tu nie ma",
          "Здесь пока пусто", "Burada henüz yok", "لا شيء هنا بعد", "这里还没有东西", "ここにはまだない", "여기 아직 없음", "यहाँ अभी कुछ नहीं", "Ma belum ada di sini",
          "Inget här än", "Ei mitään vielä", "Zatím tu nic", "ยังไม่มีอะไรที่นี่");
        A("tapToMark", "Tap a file to mark it", "Toca un archivo para marcarlo", "Toque um arquivo para marcar", "Touchez un fichier pour le cocher", "Datei antippen zum markieren", "Tocca un file per selezionarlo", "Tik op een bestand om het te markeren", "Dotknij plik, aby go zaznaczyć",
          "Коснитесь файла, чтобы выбрать его", "Bir dosyaya dokunun", "المس ملفًا لتحديده", "点按文件以选中", "ファイルをタップして選択", "파일을 탭하여 선택", "फ़ाइल चुनने के लिए टैप करें", "Ketuk file untuk menandainya",
          "Tryck på en fil för att markera", "Napauta tiedostoa valitaksesi", "Kliknite na soubor pro označení", "แตะไฟล์เพื่อทำเครื่องหมาย");
        A("markedFmt", "{0} marked · {1}", "{0} marcados · {1}", "{0} marcados · {1}", "{0} cochés · {1}", "{0} markiert · {1}", "{0} selezionati · {1}", "{0} gemarkeerd · {1}", "Zaznaczono: {0} · {1}",
          "Выбрано: {0} · {1}", "{0} işaretli · {1}", "المحدد: {0} · {1}", "已选 {0} 项 · {1}", "{0} 選択しました · {1}", "{0}개 선택됨 · {1}", "चिन्हित: {0} · {1}", "{0} ditandai · {1}",
          "{0} markerade · {1}", "Valittu {0} · {1}", "Označeno: {0} · {1}", "ทำเครื่องหมาย {0} รายการ · {1}");
        A("addToList", "Add to list", "Añadir a la lista", "Adicionar à lista", "Ajouter à la liste", "Zur Liste", "Aggiungi alla lista", "Aan lijst toevoegen", "Dodaj do listy",
          "Добавить в список", "Listeye ekle", "إضافة إلى القائمة", "加入列表", "リストに追加", "리스트에 추가", "सूची में जोड़ें", "Tambahkan ke daftar",
          "Lägg till i listan", "Lisää listaan", "Přidat do seznamu", "เพิ่มลงในรายการ");
        A("useAsDest", "Use as destination", "Usar como destino", "Usar como destino", "Utiliser comme destination", "Als Ziel verwenden", "Usa come destinazione", "Als bestemming gebruiken", "Użyj jako miejsce docelowe",
          "Использовать как папку", "Hedef olarak kullan", "استخدم كوجهة", "设为目的地", "保存先にする", "대상으로 사용", "गंतव्य के रूप में उपयोग करें", "Gunakan sebagai tujuan",
          "Använd som mål", "Käytä kohteena", "Použít jako cíl", "ใช้เป็นปลายทาง");
        A("trCreate", "Create", "Crear", "Criar", "Créer", "Erstellen", "Crea", "Maken", "Utwórz",
          "Создать", "Oluştur", "إنشاء", "创建", "作成", "만들기", "बनाएँ", "Buat",
          "Skapa", "Luo", "Vytvořit", "สร้าง");
        A("trTurnOff", "Turn off", "Apagar", "Desligar", "Éteindre", "Ausschalten", "Spegni", "Uitschakelen", "Wyłącz",
          "Выключить", "Kapat", "إيقاف", "关闭", "オフ", "끄기", "बंद करें", "Matikan",
          "Stäng av", "Sammuta", "Vypnout", "ปิด");
        A("trCreateTitle", "I want to send", "Quiero enviar", "Quero enviar", "Je veux envoyer", "Senden", "Voglio inviare", "Ik wil verzenden", "Chcę wysłać",
          "Отправить", "Göndermek", "أريد الإرسال", "我要发送", "送信する", "보내기", "मैं भेजना चाहता हूँ", "Saya ingin mengirim",
          "Skicka", "Lähetän", "Chci odeslat", "ฉันต้องการส่ง");
        A("trJoinTitle", "I want to receive", "Quiero recibir", "Quero receber", "Je veux recevoir", "Empfangen", "Voglio ricevere", "Ik wil ontvangen", "Chcę odebrać",
          "Получить", "Almak", "أريد الاستلام", "我要接收", "受信する", "받기", "मैं प्राप्त करना चाहता हूँ", "Saya ingin menerima",
          "Ta emot", "Vastaanottaa", "Chci přijmout", "ฉันต้องการรับ");
        A("trJoin", "Join", "Unirse", "Entrar", "Rejoindre", "Beitreten", "Entra", "Deelnemen", "Dołącz",
          "Подключиться", "Katıl", "انضمام", "加入", "参加", "참여", "जुड़ें", "Gabung",
          "Gå med", "Liity", "Připojit", "เข้าร่วม");
        A("trCodeHint", "Paste the code from the other device", "Pega el código del otro equipo", "Cole o código do outro dispositivo", "Collez le code de l’autre appareil", "Code vom anderen Gerät einfügen", "Incolla il codice dell’altro dispositivo", "Plak de code van het andere apparaat", "Wklej kod z drugiego urządzenia",
          "Вставьте код с другого устройства", "Diğer cihazdaki kodu yapıştır", "الصق الرمز من الجهاز الآخر", "粘贴另一台设备的代码", "他の端末のコードを貼り付け", "다른 기기의 코드를 붙여넣기", "दूसरे डिवाइस का कोड चिपकाएँ", "Tempel kode dari perangkat lain",
          "Klistra koden från den andra enheten", "Liitä koodi toiselta laitteelta", "Vložte kód z druhého zařízení", "วางรหัสจากอุปกรณ์อื่น");
        A("trJoinHint", "Scanning the QR needs the camera. If you do not have one, type or paste the code.", "Escanear el QR necesita la cámara. Si no tienes, escribe o pega el código.", "Ler o QR precisa da câmera. Se não tiver, escreva o código.", "Scanner le QR demande l’appareil photo. Sinon, tapez le code.", "Für den QR-Scan braucht es die Kamera. Sonst den Code einfügen.", "Per scansionare il QR serve la fotocamera. Se non ce l’hai, scrivi il codice.", "QR scannen kan met de camera. Zonder camera: plak de code.", "Skanowanie QR wymaga aparatu. Bez niego wklej kod.",
          "Для сканирования QR нужна камера. Без неё вставьте код.", "QR taramak için kamera gerekir. Yoksa kodu yazın.", "مسح رمز QR يحتاج كاميرا. بدونها الصق الرمز.", "扫二维码需要摄像头。没有的话粘贴代码。", "QRの読み取りにはカメラが必要です。カメラがなければコードを貼り付けてください。", "QR을 읽으려면 카메라가 필요합니다. 없으면 코드를 붙여넣으세요.", "QR स्कैन करने के लिए कैमरा चाहिए। नहीं है तो कोड चिपकाएँ", "Memindai QR memerlukan kamera. Jika tidak ada, ketik atau tempel kodenya.",
          "Att skanna QR behövs kameran. Saknar den: klistra koden.", "QR:n lukemiseen tarvitaan kameraa. Muuten liitä koodi.", "Skenování QR vyžaduje kameru. Bez ní vložte kód.", "การสแกน QR ต้องใช้กล้อง ถ้าไม่มีกล้องให้พิมพ์หรือวางรหัส");
        A("trCodeEmpty", "Paste or type the code first", "Primero pega o escribe el código", "Primeiro cole ou escreva o código", "Collez ou tapez d’abord le code", "Erst den Code einfügen", "Prima scrivi o incolla il codice", "Eerst de code plakken of typen", "Najpierw wklej lub wpisz kod",
          "Сначала вставьте или введите код", "Önce kodu yapıştırın أو yazın", "الصق الرمز أو اكتبه أولا", "先粘贴或输入代码", "先にコードを貼り付けてください", "먼저 코드를 붙여넣거나 입력하세요", "पहले कोड चिपकाएँ या लिखें", "Tempel atau ketik kode terlebih dahulu",
          "Klistra eller skriv koden först", "Liitä tai kirjoita koodi ensin", "Nejprve vložte nebo napište kód", "วางหรือพิมพ์รหัสก่อน");
        A("trHotspot", "Turn on hotspot (no router needed)", "Encender hotspot (sin router)", "Ligar hotspot (sem router)", "Activer le partage de connexion", "Hotspot einschalten (ohne Router)", "Accendi l’hotspot (senza router)", "Hotspot aanzetten (geen router nodig)", "Włącz hotspot (bez routera)",
          "Включить точку доступа", "Hotspot aç (yönlendirici gerekmez)", "تشغيل نقطة الاتصال", "开启热点（无需路由器）", "ホットスポットをオン（ルーター不要）", "핫스팟 켜기 (라우터 불필요)", "हॉटस्पॉट चालू करें (राउटर की ज़रूरत नहीं)", "Nyalakan hotspot (tanpa router)",
          "Slå på hotspot (ingen router behövs)", "Ota hotspot käyttöön (ei reitintä)", "Zapnout hotspot (bez routeru)", "เปิดฮอตสปอต (ไม่ต้องใช้เราเตอร์)");
        A("trHotspotAndroid", "On Android the hotspot is made from the quick settings of the system, so there is nothing to press here.", "En Android el hotspot se hace desde los ajustes rápidos del sistema, así que aquí no hay nada que pulsar.", "No Android o hotspot faz-se nos ajustes rápidos do sistema.", "Sur Android, le partage de connexion se fait depuis les réglages rapides.", "Unter Android wird der Hotspot über die Schnelleinstellungen gemacht.", "Su Android l’hotspot si fa dalle impostazioni rapide.", "Op Android maak je de hotspot via de snelleinstellingen.", "W Androidzie hotspot robi sie w szybkich ustawieniach.",
          "В Android хотспот включается в быстрых настройках.", "Android’da hotspot hızlı ayarlardan açılır.", "في أندرويد يتم تشغيل نقطة الاتصال من الإعدادات السريعة.", "安卓的热点在系统快捷设置里开启。", "Androidではクイック設定からオンにします。", "안드로이드에서는 시스템 빠른 설정에서 켜면 됩니다.", "एंड्रॉइड में हॉटस्पॉट तेज़ सेटिंग से चालू होता है", "Di Android, hotspot diaktifkan lewat setelan cepat sistem, jadi tidak ada tombol di sini.",
          "Op Android zet je de hotspot aan via de snelleinstellingen.", "Androidissa hotspot kytketään pika-asetuksista.", "V Androidu se hotspot zapne v rychlých nastaveních.", "บน Android เปิดฮอตสปอตได้จากการตั้งค่าด่วนของระบบ จึงไม่มีปุ่มให้กดที่นี่");
        A("trOn", "On: the other device can connect", "Encendido: el otro equipo puede conectarse", "Ligado: o outro dispositivo pode ligar-se", "Allumé : l’autre appareil peut se connecter", "An: das andere Gerät kann sich verbinden", "Acceso: l’altro dispositivo può connettersi", "Aan: het andere apparaat kan verbinden", "Włączone: drugie urządzenie może się połączyć",
          "Включено: другое устройство может подключиться", "Açık: diğer cihaz bağlanabilir", "مفعّل: يمكن للجهاز الآخر الاتصال", "已开启：对方可以连接", "オン：相手の端末が接続できます", "켜짐: 상대 기기가 연결할 수 있습니다", "चालू: दूसरा डिवाइस जुड़ सकता है", "Aktif: perangkat lain dapat terhubung",
          "På: den andra enheten kan ansluta", "Päällä: toinen laite voi liittyä", "Zapnuto: druhé zařízení se může připojit", "เปิด: อุปกรณ์อื่นเชื่อมต่อได้");
        A("trOff", "Off: turn it on to transfer", "Apagado: enciéndelo para transferir", "Desligado: ligue para transferir", "Éteint : activez-le pour transférer", "Aus: zum Transfer einschalten", "Spento: accendilo per trasferire", "Uit: zet aan om te transfereren", "Wyłączone: włącz, aby przesyłać",
          "Выключено: включите для передачи", "Kapalı: açın ve aktarın", "مطفأ: شغّله للتحويل", "未开启：打开才能传输", "オフ：オンにすると転送できます", "꺼짐: 켜야 전송됩니다", "बंद: स्थानांतरण के लिए चालू करें", "Nonaktif: nyalakan dulu untuk mentransfer",
          "Av: slå på för att skicka", "Päällä siirtoa varten", "Vypnuto: zapněte pro přenos", "ปิด: เปิดก่อนจึงจะโอนได้");
        A("trMore", "More options", "Más opciones", "Mais opções", "Plus d’options", "Weitere Optionen", "Altre opzioni", "Meer opties", "Więcej opcji",
          "Другие параметры", "Diğer seçenekler", "خيارات أخرى", "更多选项", "その他のオプション", "추가 옵션", "अन्य विकल्प", "Pilihan lainnya",
          "Fler alternativ", "Lisää vaihtoehtoja", "Další možnosti", "ตัวเลือกอื่น");
        A("trCodeBad", "That code is not a QBasWing address", "Ese código no es una dirección de QBasWing", "Esse código não é um endereço do QBasWing", "Ce code n’est pas une adresse QBasWing", "Dieser Code ist keine QBasWing-Adresse", "Questo codice non è un indirizzo QBasWing", "Deze code is geen QBasWing-adres", "Ten kod to nie adres QBasWing",
          "Это не адрес QBasWing", "Bu kod bir QBasWing adresi değil", "هذا الرمز ليس عنوان QBasWing", "此代码不是 QBasWing 地址", "このコードは QBasWing のアドレスではありません", "이 코드는 QBasWing 주소가 아닙니다", "यह कोड QBasWing पता नहीं है", "Kode itu bukan alamat QBasWing",
          "Koden är inte en QBasWing-adress", "Tämä koodi ei ole QBasWing-osoite", "Tento kód není adresa QBasWing", "รหัสนี้ไม่ใช่ที่อยู่ QBasWing");
        A("trJoined", "Connected. Choose what to send.", "Conectado. Elige qué enviar.", "Ligado. Escolha o que enviar.", "Connecté. Choisissez ce que vous voulez envoyer.", "Verbunden. Wähle aus, was gesendet wird.", "Connesso. Scegli cosa inviare.", "Verbonden. Kies wat je wilt versturen.", "Połączono. Wybierz, co wysłać.",
          "Подключено. Выберите, что отправить.", "Bağlandı. Ne göndereceğini seç.", "تم الاتصال. اختر ما تريد إرساله.", "已连接。选择要发送的内容。", "接続しました。送るものを選んでください。", "연결됨. 보낼 항목을 선택하세요.", "जुड़ गया। चुनें क्या भेजना है।", "Terhubung. Pilih yang akan dikirim.",
          "Ansluten. Välj vad som ska skickas.", "Yhdistetty. Valitse lähetettävät.", "Připojeno. Vyberte, co poslat.", "เชื่อมต่องแล้ว เลือกสิ่งที่จะส่ง");
        A("trNoAnswer", "Nothing answers at that address", "No responde en esa dirección", "Não responde nesse endereço", "Rien ne répond à cette adresse", "Unter dieser Adresse antwortet nichts", "Non risponde a quell’indirizzo", "Niets reageert op dat adres", "Nikt nie odpowiada pod tym adresem",
          "По этому адресу никто не отвечает", "Bu adreste yanıt yok", "لا يوجد رد على هذا العنوان", "该地址没有响应", "そのアドレスからは応答がありません", "해당 주소에서 응답이 없습니다.", "इस पते पर कोई जवाब नहीं है", "Tidak ada yang merespons di alamat itu",
          "Ingen svarar på den adressen", "Osoite ei vastaa", "Na této adrese nikdo neodpovídá", "ไม่มีการตอบสนุนดัวที่อยู่นี้");
        A("trPaste", "Paste code", "Pegar código", "Colar código", "Coller le code", "Code einfügen", "Incolla codice", "Code plakken", "Wklej kod",
          "Вставить код", "Kodu yapıştır", "لصق الرمز", "粘贴代码", "コードを貼り付け", "코드 붙여넣기", "कोड चिपकाएँ", "Tempel kode",
          "Klistra in kod", "Liitä koodi", "Vložit kód", "วางรหัส");
        A("trPasteFail", "There is nothing in the clipboard to paste", "No hay nada en el portapapeles", "Não há nada na área de transferência", "Rien dans le presse-papiers", "Die Zwischenablage ist leer", "Non c’è nulla negli appunti", "Het klembord is leeg", "Schowek jest pusty",
          "В буфере обмена ничего нет", "Panoda yapıştılacak bir şey yok", "لا يوجد شيء في الحافظة", "剪贴板里没有内容", "クリップボードに内容がありません", "붙여넣을 내용이 없습니다.", "क्लिपबोर्ड में कुछ नहीं है", "Tidak ada isi di papan klip",
          "Inget i urklipp att klistra in", "Leikepöydässä ei ole mitään", "Ve schránce nic není", "ไม่มีอะกราบสิ่งที่คลิปบอร์");
        A("trSend", "Send", "Enviar", "Enviar", "Envoyer", "Senden", "Invia", "Versturen", "Wyślij",
          "Отправить", "Gönder", "إرسال", "发送", "送信", "보내기", "भेजें", "Kirim",
          "Skicka", "Lähetä", "Odeslat", "ส่ง");
        A("catPhotos", "Photos", "Fotos", "Fotos", "Photos", "Fotos", "Foto", "Foto’s", "Zdjećcia",
          "Фото", "Foto’lar", "صور", "照片", "写真", "사진", "फ़ोटो", "Foto",
          "Foton", "Kuvat", "Fotografie", "รูปภาพ");
        A("catVideos", "Videos", "Vídeos", "Vídeos", "Vidéos", "Videos", "Video", "Video’s", "Filmy",
          "Видео", "Videolar", "فيديو", "视频", "動画", "동영상", "वीडियो", "Video",
          "Videor", "Videot", "Videa", "วิดีโอ");
        A("catMusic", "Music", "Música", "Música", "Musique", "Musik", "Musica", "Muziek", "Muzyka",
          "Музыка", "Müzik", "موسيقي", "音乐", "音楽", "음악", "संगीत", "Musik",
          "Musik", "Musiikki", "Hudba", "เพล");
        A("catDocs", "Documents", "Documentos", "Documentos", "Documents", "Dokumente", "Documenti", "Documenten", "Dokumenty",
          "Документы", "Belgeler", "مستندات", "文档", "文書", "문서", "दस्तावेज़", "Dokumen",
          "Dokument", "Dokumentit", "Dokumenty", "เอกสาร");
        A("catApps", "Installers", "Instaladores", "Instaladores", "Installateurs", "Installateure", "Installatori", "Installers", "Instalatory",
          "Установщики", "Kurulum dosyaları", "مثبتات", "安装包", "インストーラー", "설치 파일", "इंस्टॉलर", "Pemasang",
          "Installationsprogram", "Asentimet", "Instalátory", "ตัวติดตั้ง");
        A("catOther", "Other", "Otros", "Outros", "Autres", "Sonstige", "Altri", "Overig", "Inne",
          "Другое", "Diğer", "أخرى", "其他", "その他", "기타", "अन्य", "Lainnya",
          "Övrigt", "Muut", "Ostatní", "อื่น");
        A("sTransfer", "Transfer", "Transferir", "Transferir", "Transfert", "Übertragen", "Trasferisci", "Overdracht", "Transfer",
          "Передача", "Aktarım", "النقل", "传输", "転送", "전송", "स्थानांतरण", "Transfer",
          "Överföring", "Siirto", "Přenos", "การถ่ายโอน");
        A("trInbox", "Where received files go", "Dónde se guardan los recibidos", "Onde ficam os recebidos", "Où vont les fichiers reçus", "Wohin empfangene Dateien kommen", "Dove finiscono i file ricevuti", "Waar ontvangen bestanden heen gaan", "Gdzie trafiają odebrane pliki",
          "Куда попадают полученные файлы", "Alınan dosyalar nereye gider", "إلى أين تذهب الملفات المستلمة", "接收的文件放在哪里", "受信ファイルの保存先", "받는 파일이 저장되는 위치", "प्राप्त फ़ैलें कहां जाती हैं", "Tempat file yang diterima",
          "Var mottagna filer hamnar", "Minne vastaanotetut tiedot menevät", "Kam se ukládají přijaté soubory", "ไฟล์ที่ได้รับจะถูกเก็บไว้ที่นี่");
        A("trReset", "Reset", "Restablecer", "Repor", "Réinitialiser", "Zurücksetzen", "Ripristina", "Herstellen", "Przywróć",
          "Сбросить", "Sıfırla", "إعادة تعيين", "重置", "リセット", "초기화", "रीसेट", "Atur ulang",
          "Återställ", "Palauta", "Obnovit výchozí", "รีเซ็ต");
        A("trPort", "Port", "Puerto", "Porta", "Port", "Port", "Porta", "Poort", "Port",
          "Порт", "Bağlantı noktası", "المنفذ", "端口", "ポート", "포트", "पोर्ट", "Port",
          "Port", "Portti", "Port", "พอร์ต");
        A("trNotify", "Notify when a file arrives", "Avisar al recibir", "Avisar ao receber", "Prévenir à la réception", "Bei Empfang benachrichtigen", "Avvisa alla ricezione", "Melden bij ontvangst", "Powiadom przy odbiorze",
          "Уведомлять при получении", "Dosya gelince bildir", "إشعار عند وصول ملف", "接收时提醒", "着信時に通知", "파일 도착 시 알림", "फ़ाइल आने पर सूचना", "Beri tahu saat menerima",
          "Meddela vid mottagning", "Ilmoita vastaanotosta", "Upozornit při přijetí", "แจ้งเตือนเมื่อได้รับไฟล์");
        A("trDevName", "Name of this device", "Nombre de este equipo", "Nome deste dispositivo", "Nom de cet appareil", "Name dieses Geräts", "Nome di questo dispositivo", "Naam van dit apparaat", "Nazwa tego urządzenia",
          "Имя этого устройства", "Bu cihazın adı", "اسم هذا الجهاز", "本机名称", "この端末の名称", "이 기기 이름", "इस डिवाइस का नाम", "Nama perangkat ini",
          "Enhetens namn", "Tämän laitteen nimi", "Název tohoto zařízení", "ชื่อเครื่อนี้");
        A("transfer", "Transfer", "Transferir", "Transferir", "Transférer", "Übertragen", "Trasferisci",
          "Overdragen", "Transferuj", "Передать", "Aktar", "نقل", "传输",
          "転送", "전송", "स्थानांतरण", "Transfer", "Överför", "Siirrä", "Přenos", "ถ่ายโอน");
        A("warning", "Warning", "Advertencia", "Aviso", "Avertissement", "Warnung", "Avviso",
          "Waarschuwing", "Ostrzeżenie", "Предупреждение", "Uyarı", "تحذير", "警告",
          "警告", "경고", "चेतावनी", "Peringatan", "Varning", "Varoitus", "Varování", "คำเตือน");
        A("sSpeed", "Speed", "Velocidad", "Velocidade", "Vitesse", "Geschwindigkeit", "Velocità",
          "Snelheid", "Szybkość", "Скорость", "Hız", "السرعة", "速度",
          "速度", "속도", "गति", "Kecepatan", "Hastighet", "Nopeus", "Rychlost", "ความเร็ว");
        A("sPerformance", "Performance", "Rendimiento", "Desempenho", "Performance", "Leistung", "Prestazioni",
          "Prestaties", "Wydajność", "Производительность", "Performans", "الأداء", "性能",
          "パフォーマンス", "성능", "प्रदर्शन", "Kinerja", "Prestanda", "Suorituskyky", "Výkon", "ประสิทธิภาพ");
        A("sExplorer", "File manager", "Explorador", "Explorador", "Explorateur", "Explorer", "Esplora risorse",
          "Verkenner", "Eksplorator", "Проводник", "Dosya Gezgini", "مستكشف الملفات", "文件管理器",
          "エクスプローラー", "파일 탐색기", "फ़ाइल प्रबंधक", "Pengelola Berkas", "Filhanterare", "Tiedostonhallinta", "Správce souborů", "ตัวจัดการไฟล์");
        A("tabCopy", "Copy", "Copiar", "Copiar", "Copier", "Kopieren", "Copia",
          "Kopieren", "Kopiuj", "Копировать", "Kopyala", "نسخ", "复制",
          "コピー", "복사", "कॉपी", "Salin", "Kopiera", "Kopioi", "Kopírovat", "คัดลอก");
        A("tabTransfer", "Transfer", "Transferir", "Transferir", "Transférer", "Übertragen", "Trasferisci",
          "Overdragen", "Transferuj", "Передать", "Aktar", "نقل", "传输",
          "転送", "전송", "स्थानांतरण", "Transfer", "Överför", "Siirrä", "Přenos", "ถ่ายโอน");
        A("tabAbout", "About", "Acerca de", "Sobre", "À propos", "Über", "Informazioni",
          "Over", "O programie", "О программе", "Hakkında", "حول", "关于",
          "概要", "정보", "परिचय", "Tentang", "Om", "Tietoja", "O aplikaci", "เกี่ยวกับ");

        A("selectAll", "Select all", "Seleccionar todo", "Selecionar tudo", "Tout sélectionner", "Alle auswählen", "Seleziona tutto",
          "Alles selecteren", "Zaznacz wszystko", "Выбрать все", "Tümünü seç", "تحديد الكل", "全选",
          "すべて選択", "모두 선택", "सभी चुनें", "Pilih semua", "Markera alla", "Valitse kaikki", "Vybrat vše", "เลือกทั้งหมด");
        A("deselectAll", "Deselect", "Deseleccionar", "Desmarcar", "Désélectionner", "Abwählen", "Deseleziona",
          "Deselecteren", "Odznacz wszystko", "Снять выделение", "Seçimi kaldır", "إلغاء التحديد", "取消选择",
          "選択解除", "선택 해제", "चयन हटाएँ", "Batalkan pilih", "Avmarkera inte", "Poista valinta", "Zrušit výběr", "ยกเลิกการเลือก");
        A("removeFromList", "Remove from list", "Quitar de la lista", "Remover da lista", "Retirer de la liste", "Aus der Liste entfernen", "Rimuovi dalla lista",
          "Uit lijst verwijderen", "Usuń z listy", "Убрать из списка", "Listeden çıkar", "إزالة من القائمة", "从列表移除",
          "リストから削除", "목록에서 제거", "सूची से हटाएँ", "Hapus dari daftar", "Ta bort från listan", "Poista luettelosta", "Odebrat ze seznamu", "ลบออกจากรายการ");
        A("delete", "Delete", "Eliminar", "Excluir", "Supprimer", "Löschen", "Elimina",
          "Verwijderen", "Usuń", "Удалить", "Sil", "حذف", "删除",
          "削除", "삭제", "हटाएँ", "Hapus", "Ta bort", "Poista", "Smazat", "ลบ");
        A("openFolder", "Open folder", "Abrir carpeta", "Abrir pasta", "Ouvrir le dossier", "Ordner öffnen", "Apri cartella",
          "Map openen", "Otwórz folder", "Открыть папку", "Klasörü aç", "فتح المجلد", "打开文件夹",
          "フォルダーを開く", "폴더 열기", "फ़ोल्डर खोलें", "Buka folder", "Öppna mapp", "Avaa kansio", "Otevřít složku", "เปิดโฟลเดอร์");
        A("reverseList", "Reverse order", "Invertir orden", "Inverter ordem", "Inverser l'ordre", "Reihenfolge umkehren", "Inverti ordine",
          "Volgorde omkeren", "Odwróć kolejność", "Обратный порядок", "Sırayı ters çevir", "عكس الترتيب", "反转顺序",
          "順序を逆にする", "순서 뒤집기", "क्रम उलटें", "Balik urutan", "Vänd ordning", "Käännä järjestys", "Obrátit pořadí", "กลับลำดับ");
        A("oneCopy", "Copy this", "Copiar este", "Copiar este", "Copier celui-ci", "Diese kopieren", "Copia questo",
          "Deze kopiëren", "Kopiuj ten", "Скопировать этот", "Bunu kopyala", "نسخ هذا", "复制这个",
          "これをコピー", "이것 복사", "इसे कॉपी करें", "Salin ini", "Kopiera den här", "Kopioi tämä", "Kopírovat tento", "คัดลอกรายการนี้");
        A("oneMove", "Move this", "Mover este", "Mover este", "Déplacer celui-ci", "Diese verschieben", "Sposta questo",
          "Deze verplaatsen", "Przenieś ten", "Переместить этот", "Bunu taşı", "نقل هذا", "移动这个",
          "これを移動", "이것 이동", "इसे ले जाएँ", "Pindahkan ini", "Flytta den här", "Siirrä tämä", "Přesunout tento", "ย้ายรายการนี้");
        A("trSort", "Sort what arrives into folders by type", "Ordenar lo recibido en carpetas por tipo", "Organizar o que chega em pastas por tipo", "Classer ce qui arrive dans des dossiers par type", "Empfangenes nach Typ in Ordner sortieren", "Ordina ciò che arriva in cartelle per tipo", "Sorteer wat binnenkomt in mappen per soort", "Sortuj przychodzące pliki w foldery według typu",
          "Сортировать полученное по папкам и типу", "Gelenenleri türüne göre klasörlere ayır", "ترتيب الملفات الواردة في مجلدات حسب النوع", "按类型将收到的文件分到不同文件夹", "受信ファイルを種類別のフォルダに整理", "받는 파일을 종볌별 폴더로 정리", "आने वाली फ़ैलों को प्रकार के अनुसार फ़ोल्डर में", "Kelompokkan yang masuk ke folder menurut jenis",
          "Sortera in i mappar efter typ", "Jaa saapuneet kansioihin tyypin mukaan", "Řadit přijaté soubory do složek podle typu", "จัดเก็บไฟล์ที่ได้รับเป็นโฟลเดอร์ตามชนิด");
        A("volDownloads", "Downloads", "Descargas", "Transferências", "Téléchargements", "Downloads", "Download", "Downloads", "Pobrane",
          "Загрузки", "İndirilenler", "التنزيلات", "下载", "ダウンロード", "다운로드", "डाउनलोड", "Unduhan",
          "Nedladdningar", "Lataukset", "Stažení", "ดาวน์โหลด");
        A("volStorage", "Phone storage", "Almacenamiento del móvil", "Armazenamento do telemóvel", "Stockage du téléphone", "Telefonspeicher", "Archiviazione del telefono", "Telefoonopslag", "Pamięć telefonu",
          "Память телефона", "Telefon belleği", "ذاكرة الهاتف", "手机存储", "端末のストレージ", "기기 저장소", "फ़ोन स्टोरेज", "Penyimpanan ponsel",
          "Telefonlagring", "Puhelimen tallennus", "Úložiště telefonu", "พื้นที่โทรศัพท์");
        A("volSdUsb", "SD / USB", "SD / USB", "SD / USB", "SD / USB", "SD / USB", "SD / USB", "SD / USB", "SD / USB",
          "SD / USB", "SD / USB", "SD / USB", "SD / USB", "SD / USB", "SD / USB", "SD / USB", "SD / USB",
          "SD / USB", "SD / USB", "SD / USB", "SD / USB");
        A("trAskFolder", "Where do you want received files to go?", "¿Dónde quieres que se guarden los archivos que te envíen?", "Onde queres que fiquem os ficheiros enviados?", "Où voulez-vous enregistrer les fichiers reçus ?", "Wohin sollen empfangene Dateien gespeichert werden?", "Dove vuoi salvare i file ricevuti?", "Waar wil je ontvangen bestanden opslaan?", "Gdzie zapisywać otrzymane pliki?",
          "Куда сохранять полученные файлы?", "Alınan dosyalar nereye kaydedilsin?", "أين تريد حفظ الملفات الواردة؟", "收到的文件要保存在哪里？", "受け取ったファイルをどこに保存しますか？", "받은 파일을 어디에 저장할까요?", "प्राप्त फ़ाइलें कहाँ सहेजनी हैं?", "Di mana file yang diterima disimpan?",
          "Var vill du spara mottagna filer?", "Minne haluat tallentaa vastaanotetut tiedot?", "Kam uložit přijaté soubory?", "ต้องการเก็บไฟล์ที่ได้รับไว้ที่ใด");
        A("trSendFolder", "Send folder…", "Enviar carpeta…", "Enviar pasta…", "Envoyer un dossier…", "Ordner senden…", "Invia cartella…", "Map versturen…", "Wyślij folder…",
          "Отправить папку…", "Klasör gönder…", "إرسال مجلد…", "发送文件夹…", "フォルダーを送信…", "폴더 보내기…", "फ़ोल्डर भेजें…", "Kirim folder…",
          "Skicka mapp…", "Lähetä kansio…", "Odeslat složku…", "ส่งโฟลเดอร์…");
        A("trSelDirs", "Select {0} files · {1} folders", "Seleccionar {0} archivos · {1} carpetas", "Selecionar {0} ficheiros · {1} pastas", "Sélectionner {0} fichiers · {1} dossiers", "{0} Dateien · {1} Ordner auswählen", "Seleziona {0} file · {1} cartelle", "{0} bestanden · {1} mappen selecteren", "Wybierz {0} plików · {1} folderów",
          "Выбрать {0} файлов · {1} папок", "{0} dosya · {1} klasör seç", "اختر {0} ملفًا · {1} مجلدًا", "选择 {0} 个文件 · {1} 个文件夹", "{0} 個のファイル · {1} 個のフォルダーを選択", "파일 {0}개 · 폴더 {1}개 선택", "{0} फ़ाइलें · {1} फ़ोल्डर चुनें", "Pilih {0} file · {1} folder",
          "Välj {0} filer · {1} mappar", "Valitse {0} tiedostoa · {1} kansiota", "Vybrat {0} souborů · {1} složek", "เลือก {0} ไฟล์ · {1} โฟลเดอร์");
        A("sending", "Sending", "Enviando", "Enviando", "Envoi", "Senden", "Invio", "Verzenden", "Wysyłanie",
          "Отправка", "Gönderiliyor", "جارٍ الإرسال", "正在发送", "送信中", "전송 중", "भेजा जा रहा है", "Mengirim",
          "Skickar", "Lähetetään", "Odesílání", "กำลังส่ง");
        A("receiving", "Receiving", "Recibiendo", "Recebendo", "Réception", "Empfang", "Ricezione", "Ontvangen", "Odbieranie",
          "Приём", "Alınıyor", "جارٍ الاستلام", "正在接收", "受信中", "수신 중", "प्राप्त हो रहा है", "Menerima",
          "Tar emot", "Vastaanotto", "Příjem", "กำลังรับ");
        A("pausedTag", "Paused", "En pausa", "Em pausa", "En pause", "Pausiert", "In pausa", "Gepauzeerd", "Wstrzymano",
          "Пауза", "Duraklatıldı", "متوقف مؤقتًا", "已暂停", "一時停止", "일시 중지", "रुका हुआ", "Dijeda",
          "Pausad", "Keskeytetty", "Pozastaveno", "หยุดชั่วคราว");
        A("popMore", "More details", "Más detalles", "Mais detalhes", "Plus de détails", "Mehr Details", "Altro dettaglio", "Meer details", "Więcej szczegółów",
          "Подробнее", "Ayrıntılar", "تفاصيل أخرى", "更多详情", "詳細", "자세히", "और विवरण", "Detail lainnya",
          "Mer detaljer", "Lisätiedot", "Podrobnosti", "รายละเอียด");
        A("popRetry", "Retry failed", "Reintentar fallidos", "Repetir falhos", "Réessayer les échecs", "Fehlgeschlagene wiederholen", "Riprova falliti", "Mislukte opnieuw", "Ponów nieudane",
          "Повторить сбойные", "Başarısızları yeniden dene", "إعادة محاولة الفاشل", "重试失败项", "失敗した項目を再試行", "실패 항목 다시 시도", "विफल पुनः प्रयास", "Ulangi yang gagal",
          "Försök misslyckade igen", "Yrityksetään epäonnistuneet", "Zkusit znovu neúspěšné", "ลองใหม่สำหรับที่ล้มเหลว");
        A("trFolder", "Folder", "Carpeta", "Pasta", "Dossier", "Ordner", "Cartella", "Map", "Folder",
          "Папка", "Klasör", "مجلد", "文件夹", "フォルダー", "폴더", "फ़ोल्डर", "Folder",
          "Mapp", "Kansio", "Složka", "โฟลเดอร์");
        A("refresh", "Refresh", "Actualizar", "Atualizar", "Actualiser", "Aktualisieren", "Aggiorna", "Vernieuwen", "Odśwież",
          "Обновить", "Yenile", "تحديث", "刷新", "更新", "새로 고침", "रिफ्रेश", "Segarkan",
          "Uppdatera", "Päivitä", "Obnovit", "รีเฟรช");
        A("trDropHint", "You can also drop files or folders here", "También puedes soltar aquí archivos o carpetas", "Também pode largar aqui ficheiros ou pastas", "Vous pouvez aussi déposer ici des fichiers ou des dossiers", "Dateien oder Ordner kann man hier auch ablegen", "Qui puoi trascinare qui file o cartelle", "Je kunt hier ook bestanden of mappen neerzetten", "Możesz też upuścić tu pliki lub foldery",
          "Можно перетащить сюда файлы или папки", "Dosyaları veya klasörleri buraya da sürükleyebilirsin", "يمكنك أيضًا إسقاط الملفات أو المجلدات هنا", "也可以把文件或文件夹拖到这里", "ファイルやフォルダーをここにドラッグできます", "여기에 파일이나 폴더를 끌어다 놓을 수 있습니다", "यहाँ फ़ाइलें या फ़ोल्डर छोड़े जा सकते हैं", "Anda juga boleh seret berkas atau folder ke sini",
          "Du kan också släppa filer eller mappar här", "Voit pudottaa tähän tiedostoja tai kansioita", "Sem můžete přetáhnout soubory či složky", "คุณสามารถลากไฟล์หรือโฟลเดอร์มาวางที่นี่ได้");
    }
}