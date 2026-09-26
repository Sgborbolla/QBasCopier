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
        A("tabErrors", "Errors", "Errores", "Erros", "Erreurs", "Fehler",
          "Errori", "Fouten", "Błędy", "Ошибки", "Hatalar",
          "الأخطاء", "错误", "エラー", "오류", "त्रुटियाँ", "Kesalahan", "Fel", "Virheet", "Chyby", "ข้อผิดพลาด");
        A("tabInterface", "Interface", "Interfaz", "Interface", "Interface", "Schnittstelle",
          "Interfaccia", "Interface", "Interfejs", "Интерфейс", "Arayüz",
          "الواجهة", "界面", "画面", "화면", "इंटरफ़ेस", "Antarmuka", "Gränssnitt", "Käyttöliittymä", "Rozhraní", "อินเทอร์เฟซ");
        A("tabEngine", "Engine", "Motor", "Motor", "Moteur", "Motor",
          "Motore", "Motor", "Silnik", "Движок", "Motor",
          "المحرك", "引擎", "エンジン", "엔진", "इंजन", "Mesin", "Motor", "Moottori", "Jádro", "เอนจิน");
        A("tabHistory", "History", "Historial", "Histórico", "Historique", "Verlauf",
          "Cronologia", "Geschiedenis", "Historia", "История", "Geçmiş",
          "السجل", "历史", "履歴", "기록", "इतिहास", "Riwayat", "Historik", "Historia", "Historie", "ประวัติ");
        A("colName", "File", "Archivo", "Arquivo", "Fichier", "Datei",
          "File", "Bestand", "Plik", "Файл", "Dosya",
          "الملف", "文件", "ファイル", "파일", "फ़ाइल", "Berkas", "Fil", "Tiedosto", "Soubor", "ไฟล์");
        A("colSize", "Size", "Tamaño", "Tamanho", "Taille", "Größe",
          "Dimensione", "Grootte", "Rozmiar", "Размер", "Boyut",
          "الحجم", "大小", "サイズ", "크기", "आकार", "Ukuran", "Storlek", "Koko", "Velikost", "ขนาด");
        A("colState", "State", "Estado", "Estado", "État", "Status",
          "Stato", "Status", "Stan", "Состояние", "Durum",
          "الحالة", "状态", "状態", "상태", "स्थिति", "Status", "Status", "Tila", "Stav", "สถานะ");
        A("colPercent", "Progress", "Progreso", "Progresso", "Progression", "Fortschritt",
          "Avanzamento", "Voortgang", "Postęp", "Прогресс", "İlerleme",
          "التقدم", "进度", "進捗", "진행", "प्रगति", "Kemajuan", "Förlopp", "Edistyminen", "Průběh", "ความคืบหน้า");
        A("colSpeed", "Speed", "Velocidad", "Velocidade", "Vitesse", "Geschwindigkeit",
          "Velocità", "Snelheid", "Prędkość", "Скорость", "Hız",
          "السرعة", "速度", "速度", "속도", "गति", "Kecepatan", "Hastighet", "Nopeus", "Rychlost", "ความเร็ว");
        A("currentFile", "Current file", "Archivo actual", "Arquivo atual", "Fichier en cours", "Aktuelle Datei",
          "File corrente", "Huidig bestand", "Bieżący plik", "Текущий файл", "Geçerli dosya",
          "الملف الحالي", "当前文件", "現在のファイル", "현재 파일", "वर्तमान फ़ाइल", "Berkas saat ini",
          "Aktuell fil", "Nykyinen tiedosto", "Aktuální soubor", "ไฟล์ปัจจุบัน");
        A("total", "Total", "Total", "Total", "Total", "Gesamt",
          "Totale", "Totaal", "Razem", "Всего", "Toplam",
          "الإجمالي", "总计", "合計", "총계", "कुल", "Total", "Totalt", "Yhteensä", "Celkem", "รวม");
        A("files", "files", "archivos", "arquivos", "fichiers", "Dateien",
          "file", "bestanden", "plików", "файлов", "dosya",
          "ملفات", "个文件", "ファイル", "파일", "फ़ाइलें", "berkas", "filer", "tiedostoa", "souborů", "ไฟล์");
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
        A("stateSkipped", "Skipped", "Omitido", "Ignorado", "Ignoré", "Übersprungen",
          "Saltato", "Overgeslagen", "Pominięty", "Пропущено", "Atlandı",
          "تم التجاهل", "已跳过", "スキップ", "건너뜀", "छोड़ा गया", "Dilewati", "Hoppades", "Ohitettu", "Přeskočeno", "ข้าม");
        A("stateCancelled", "Cancelled", "Cancelado", "Cancelado", "Annulé", "Abgebrochen",
          "Annullato", "Geannuleerd", "Anulowano", "Отменено", "İptal edildi",
          "ملغي", "已取消", "キャンセル済み", "취소됨", "रद्द", "Dibatalkan", "Avbruten", "Peruttu", "Zrušeno", "ถูกยกเลิก");
        A("colTitle", "File already exists", "El archivo ya existe", "O arquivo já existe", "Le fichier existe déjà",
          "Die Datei existiert bereits", "Il file esiste già", "Het bestand bestaat al", "Plik już istnieje",
          "Файл уже существует", "Dosya zaten var", "الملف موجود بالفعل", "文件已存在", "ファイルは既に存在します",
          "파일이 이미 있습니다", "फ़ाइल पहले से मौजूद है", "Berkas sudah ada", "Filen finns redan",
          "Tiedosto on jo olemassa", "Soubor již existuje", "ไฟล์มีอยู่แล้ว");
        A("whatDo", "What do you want to do with this file?", "¿Qué quiere hacer con este archivo?",
          "O que deseja fazer com este arquivo?", "Que faire de ce fichier ?", "Was möchten Sie mit dieser Datei tun?",
          "Cosa vuoi fare con questo file?", "Wat wilt u met dit bestand doen?", "Co chcesz zrobić z tym plikiem?",
          "Что сделать с этим файлом?", "Bu dosyayla ne yapmak istiyorsunuz?", "ماذا تريد أن تفعل بهذا الملف؟",
          "您想对这款文件做什么？", "このファイルをどうしますか？", "이 파일로 무엇을 하시겠습니까?",
          "आप इस फ़ाइल के साथ क्या करना चाहते हैं?", "Apa yang ingin Anda lakukan dengan berkas ini?",
          "Vad vill du göra med den här filen?", "Mitä haluat tehdä tälle tiedostolle?", "Co chcete udělat s tímto souborem?",
          "คุณต้องการทำอะไรกับไฟล์นี้?");
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
        A("closeApp", "Close to tray", "Cerrar a bandeja", "Fechar para a bandeja", "Fermer dans la zone de notification",
          "In den Infobereich schließen", "Chiudi nell'area di notifica", "Sluiten naar systeemvak",
          "Zamknij do zasobnika", "Закрывать в трей", "Tepsiye kapat", "إغلاق إلى الدرج", "关闭到托盘",
          "トレイに閉じる", "트레이로 닫기", "ट्रे में बंद करें", "Tutup ke baki", "Stäng till fack",
          "Sulje ilmaisinalueelle", "Zavřít na lištu", "ปิดไปถาด");
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
        A("uninstall", "Remove integration", "Quitar integración", "Remover integração", "Désinstaller l'intégration",
          "Integration entfernen", "Rimuovi integrazione", "Integratie verwijderen", "Usuń integrację",
          "Удалить интеграцию", "Entegrasyonu kaldır", "إزالة التكامل", "移除集成", "連携を削除",
          "통합 제거", "एकीकरण हटाएं", "Lepas integrasi", "Ta bort integration", "Poista integraatio",
          "Odebrat integraci", "นำการผสานออก");
        A("sLanguage", "Language", "Idioma", "Idioma", "Langue", "Sprache",
          "Lingua", "Taal", "Język", "Язык", "Dil",
          "اللغة", "语言", "言語", "언어", "भाषा", "Bahasa", "Språk", "Kieli", "Jazyk", "ภาษา");
        A("sStartup", "Startup", "Inicio", "Inicialização", "Démarrage", "Start",
          "Avvio", "Opstarten", "Uruchamianie", "Запуск", "Başlangıç",
          "بدء التشغيل", "启动", "起動", "시작", "प्रारंभ", "Mulai", "Start", "Käynnistys", "Po spuštění", "เริ่มต้น");
        A("sUI", "Interface", "Interfaz", "Interface", "Interface", "Schnittstelle",
          "Interfaccia", "Interface", "Interfejs", "Интерфейс", "Arayüz",
          "الواجهة", "界面", "畫面", "화면", "इंटरफ़ेस", "Antarmuka", "Gränssnitt", "Käyttöliittymä", "Rozhraní", "อินเทอร์เฟซ");
        A("sDefaults", "Copies & moves defaults", "Valores predeterminados", "Padrões de cópia/movimentação",
          "Copies et déplacements par défaut", "Standard für Kopien/Verschieben", "Predefiniti per copie/spostamenti",
          "Standaard kopiëren/verplaatsen", "Domyślne dla kopii/przenoszenia", "Параметры по умолчанию",
          "Kopyalama/taşıma varsayılanları", "إعدادات النسخ والنقل الافتراضية", "复制/移动默认设置",
          "コピー/移動の既定", "복사/이동 기본값", "कॉपी/मूव डिफ़ॉल्ट", "Default salin/pindah",
          "Standardinställningar", "Kopioinnin/siirron oletukset", "Výchozí kopie/přesun",
          "ค่าเริ่มต้นการคัดลอก/ย้าย");
        A("sBehavior", "Copies & moves behavior", "Comportamiento", "Comportamento", "Comportement", "Verhalten",
          "Comportamento", "Gedrag", "Zachowanie", "Поведение", "Davranış",
          "السلوك", "行为", "動作", "동작", "व्यवहार", "Perilaku", "Beteende", "Käyttäytyminen", "Chování", "พฤติกรรม");
        A("sLog", "Error log", "Registro de errores", "Registro de erros", "Journal des erreurs", "Fehlerprotokoll",
          "Registro errori", "Foutenlogboek", "Dziennik błędów", "Журнал ошибок", "Hata günlüğü",
          "سجل الأخطاء", "错误日志", "エラーログ", "오류 로그", "त्रुटि लॉग", "Log kesalahan", "Fel logg",
          "Virheloki", "Protokol chyb", "บันทึกข้อผิดพลาด");
        A("sAdvanced", "Advanced", "Avanzado", "Avançado", "Avancé", "Erweitert",
          "Avanzate", "Geavanceerd", "Zaawansowane", "Дополнительно", "Gelişmiş",
          "متقدم", "高级", "詳細", "고급", "उन्नत", "Lanjutan", "Avancerat", "Lisäasetukset", "Pokročilé", "ขั้นสูง");
        A("ok", "OK", "Aceptar", "OK", "OK", "OK",
          "OK", "OK", "OK", "ОК", "Tamam",
          "حسناً", "确定", "OK", "확인", "ठीक", "OK", "OK", "OK", "OK", "ตกลง");
        A("apply", "Apply", "Aplicar", "Aplicar", "Appliquer", "Übernehmen",
          "Applica", "Toepassen", "Zastosuj", "Применить", "Uygula",
          "تطبيق", "应用", "適用", "적용", "लागू", "Terapkan", "Tillämpa", "Käytä", "Použít", "นำไปใช้");
        A("cancelGeneric", "Cancel", "Cancelar", "Cancelar", "Annuler", "Abbrechen",
          "Annulla", "Annuleren", "Anuluj", "Отмена", "İptal",
          "إلغاء", "取消", "キャンセル", "취소", "रद्द", "Batal", "Avbryt", "Peruuta", "Zrušit", "ยกเลิก");

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
        A("enabled", "Enable", "Activar", "Ativar", "Activer", "Aktivieren",
          "Attiva", "Inschakelen", "Włącz", "Включить", "Etkinleştir",
          "تفعيل", "启用", "有効", "활성화", "सक्षम करें", "Aktifkan",
          "Aktivera", "Käytä", "Povolit", "เปิดใช้");
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
        A("loading", "Preparing…", "Preparando…", "Preparando…", "Préparation…", "Vorbereitung…",
          "Preparazione…", "Voorbereiden…", "Przygotowanie…", "Подготовка…", "Hazırlanıyor…",
          "جارٍ التحضير…", "准备中…", "準備中…", "준비 중…", "तैयारी…", "Menyiapkan…",
          "Förbereder…", "Valmistellaan…", "Příprava…", "กำลังเตรียม…");
        A("overwriteAll", "Overwrite all", "Sobrescribir todo", "Sobrescrever todos", "Tout écraser",
          "Alle überschreiben", "Sovrascrivi tutto", "Alles overschrijven", "Nadpisz wszystko",
          "Перезаписать всё", "Tümünü üzerine yaz", "استبدال الكل", "全部覆盖",
          "すべて上書き", "모두 덮어쓰기", "सब अधिलेखित", "Timpa semua",
          "Skriv över alla", "Korvaa kaikki", "Přepsat vše", "เขียนทับทั้งหมด");
        A("skipAll", "Skip all", "Omitir todo", "Ignorar todos", "Tout ignorer", "Alle überspringen",
          "Salta tutto", "Alles overslaan", "Pomiń wszystko", "Пропустить всё", "Tümünü atla",
          "تجاهل الكل", "全部跳过", "すべてスキップ", "모두 건너뛰기", "सब छोड़ें", "Lewati semua",
          "Hoppa över alla", "Ohita kaikki", "Přeskočit vše", "ข้ามทั้งหมด");
        A("resumeAll", "Resume all", "Reanudar todo", "Retomar todos", "Tout reprendre", "Alle fortsetzen",
          "Riprendi tutto", "Alles hervatten", "Wznów wszystko", "Продолжить всё", "Tümünü sürdür",
          "استئناف الكل", "全部续传", "すべて再開", "모두 이어서", "सब फिर से", "Lanjutkan semua",
          "Fortsätt alla", "Jatka kaikki", "Pokračovat u všech", "ดำเนินการทั้งหมด");
        A("renameAll", "Rename all", "Renombrar todo", "Renomear todos", "Tout renommer", "Alle umbenennen",
          "Rinomina tutto", "Alles hernoemen", "Zmień nazwy wszystkich", "Переименовать всё",
          "Tümünü yeniden adlandır", "إعادة تسمية الكل", "全部重命名", "すべて名前変更",
          "모두 이름 바꾸기", "सब नाम बदलें", "Ganti nama semua", "Byt namn på alla",
          "Nimeä kaikki uudelleen", "Přejmenovat vše", "เปลี่ยนชื่อทั้งหมด");
        A("applyAlways", "Always", "Siempre", "Sempre", "Toujours", "Immer",
          "Sempre", "Altijd", "Zawsze", "Всегда", "Her zaman",
          "دائماً", "始终", "常に", "항상", "हमेशा", "Selalu",
          "Alltid", "Aina", "Vždy", "เสมอ");
        A("errTitle", "Copy failed", "Error al copiar", "Falha ao copiar", "Échec de la copie",
          "Kopieren fehlgeschlagen", "Errore di copia", "Fout bij kopiëren", "Błąd kopiowania",
          "Ошибка копирования", "Kopyalama hatası", "فشل النسخ", "复制失败",
          "コピーに失敗", "복사 실패", "कॉपी विफल", "Gagal menyalin",
          "Kopiering misslyckades", "Kopiointi epäonnistui", "Kopírování se nezdařilo", "การคัดลอกล้มเหลว");
        A("errWhat", "What do you want to do?", "¿Qué desea hacer?", "O que deseja fazer?",
          "Que voulez-vous faire ?", "Was möchten Sie tun?", "Cosa vuoi fare?", "Wat wilt u doen?",
          "Co chcesz zrobić?", "Что сделать?", "Ne yapmak istersiniz?", "ماذا تريد أن تفعل؟",
          "你想怎么做？", "どうしますか？", "어떻게 할까요?", "आप क्या करना चाहते हैं?",
          "Apa yang ingin dilakukan?", "Vad vill du göra?", "Mitä haluat tehdä?", "Co chcete udělat?",
          "คุณต้องการทำอย่างไร?");
        A("errValue", "Error details", "Detalles del error", "Detalhes do erro", "Détails de l'erreur",
          "Fehlerdetails", "Dettagli errore", "Foutdetails", "Szczegóły błędu", "Подробности ошибки",
          "Hata ayrıntıları", "تفاصيل الخطأ", "错误详情", "エラー詳細", "오류 세부정보", "त्रुटि विवरण",
          "Detail kesalahan", "Felinformation", "Virhetiedot", "Detaily chyby", "รายละเอียดข้อผิดพลาด");
        A("errCancelAll", "Cancel copy", "Cancelar copia", "Cancelar cópia", "Annuler la copie",
          "Kopie abbrechen", "Annulla copia", "Kopie annuleren", "Anuluj kopię",
          "Отменить копирование", "Kopyalamayı iptal et", "إلغاء النسخ", "取消复制",
          "コピーをキャンセル", "복사 취소", "कॉपी रद्द करें", "Batalkan salin",
          "Avbryt kopiering", "Peruuta kopiointi", "Zrušit kopírování", "ยกเลิกการคัดลอก");
        // ---- Settings pages ----
        A("retryInterval", "Retry interval", "Intervalo de reintento", "Intervalo de repetição",
          "Intervalle de réessai", "Wiederholungsintervall", "Intervallo nuovo tentativo",
          "Interval opnieuw", "Opóźnienie ponowienia", "Интервал повтора", "Tekrar deneme aralığı",
          "فترة إعادة المحاولة", "重试间隔", "再試行間隔", "재시도 간격", "पुनः प्रयास अंतराल",
          "Interval pengulangan", "Försöksintervall", "Uudelleenyrityksen väli",
          "Interval opakování", "ช่วงลองใหม่");
        A("msUnit", "ms", "ms", "ms", "ms", "ms", "ms", "ms", "ms", "мс", "ms",
          "مللي ثانية", "毫秒", "ms", "ms", "मि.से.", "ms", "ms", "ms", "ms", "มิลลิวินาที");
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
        A("errRetryD", "Retry", "Reintentar", "Tentar novamente", "Réessayer", "Wiederholen",
          "Riprova", "Opnieuw", "Ponów", "Повторить", "Yeniden dene",
          "إعادة المحاولة", "重试", "再試行", "재시도", "पुनः प्रयास", "Ulangi",
          "Försök igen", "Yritä uudelleen", "Zkusit znovu", "ลองอีกครั้ง");
        A("errSkipD", "Skip", "Omitir", "Ignorar", "Ignorer", "Überspringen",
          "Salta", "Overslaan", "Pomiń", "Пропустить", "Atla",
          "تجاهل", "跳过", "スキップ", "건너뛰기", "छोड़ें", "Lewati",
          "Hoppa", "Ohita", "Přeskočit", "ข้าม");
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
        A("prIdle", "Idle", "Baja", "Baixa", "Basse", "Niedrig",
          "Bassa", "Laag", "Niska", "Низкий", "Düşük",
          "منخفض", "低", "低", "낮음", "निम्न", "Rendah",
          "Låg", "Matala", "Nízká", "ต่ำ");
        A("prNormal", "Normal", "Normal", "Normal", "Normale", "Normal",
          "Normale", "Normaal", "Normalny", "Средний", "Normal",
          "عادي", "正常", "通常", "보통", "सामान्य", "Normal",
          "Normal", "Normaali", "Normální", "ปกติ");
        A("prHigh", "High", "Alta", "Alta", "Haute", "Hoch",
          "Alta", "Hoog", "Wysoka", "Высокий", "Yüksek",
          "مرتفع", "高", "高", "높음", "उच्च", "Tinggi",
          "Hög", "Korkea", "Vysoká", "สูง");
        A("windowUpdate", "Window update every", "Actualizar ventana cada", "Atualizar janela a cada",
          "Rafraîchir la fenêtre toutes les", "Fenster aktualisieren alle", "Aggiorna finestra ogni",
          "Venster bijwerken elke", "Odświeżaj okno co", "Обновление окна каждые",
          "Pencereyi güncelle her", "تحديث النافذة كل", "窗口更新间隔",
          "ウィンドウ更新間隔", "창 업데이트 주기", "विंडो अपडेट हर", "Perbarui jendela tiap",
          "Uppdatera fönstret varje", "Päivitä ikkuna joka", "Aktualizace okna každých", "อัปเดตหน้าต่างทุก");
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
        A("fastSpace", "Quick free-space check", "Verificación rápida de espacio",
          "Verificação rápida de espaço", "Vérif. rapide d'espace", "Schnelle Speicherprüfung",
          "Controllo spazio rapido", "Snelle schijfcontrole", "Szybkie sprawdzanie miejsca",
          "Быстрая проверка места", "Hızlı alan kontrolü", "فحص سريع للمساحة",
          "快速检查空间", "空き容量を高速確認", "여유 공간 빠른 확인", "तेज़ स्थान जाँच",
          "Cek ruang cepat", "Snabb ledigt utrymme", "Nopea tilan tarkistus",
          "Rychlá kontrola místa", "ตรวจพื้นที่อย่างรวดเร็ว");
        A("skipAge", "Skip age check on resume", "No comprobar fecha al reanudar",
          "Não verificar data ao retomar", "Ignorer l'âge au redémarrage", "Datum beim Fortsetzen ignorieren",
          "Ignora data alla ripresa", "Datum negeren bij hervatten", "Pomiń sprawdzanie daty",
          "Не проверять дату при возобновлении", "Sürdürürken tarihi atla",
          "تجاهل التاريخ عند الاستئناف", "续传时跳过日期检查",
          "再開時に日付を無視", "재개 시 날짜 무시", "फिर से शुरू पर तिथि अनदेखा",
          "Abaikan tanggal saat melanjutkan", "Hoppa över datumkontroll", "Ohita päivämäärän tarkistus",
          "Přeskočit kontrolu data", "ข้ามการตรวจวันที่เมื่อดำเนินต่อ");
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
        A("chooseLanguage", "Interface language", "Idioma de la interfaz", "Idioma da interface",
          "Langue de l'interface", "Oberflächensprache", "Lingua interfaccia", "Interfacetaal",
          "Język interfejsu", "Язык интерфейса", "Arayüz dili", "لغة الواجهة",
          "界面语言", "画面言語", "인터페이스 언어", "इंटरफ़ेस भाषा",
          "Bahasa antarmuka", "Gränssnittsspråk", "Käyttöliittymän kieli", "Jazyk rozhraní",
          "ภาษาส่วนต่อประสาน");
        // ---- History columns ----
        A("histFrom", "From", "De", "De", "Depuis", "Von",
          "Da", "Van", "Z", "Из", "Nereden",
          "من", "来源", "元", "원본", "से", "Dari",
          "Från", "Mistä", "Od", "จาก");
        A("histTo", "To", "Hacia", "Para", "Vers", "Nach",
          "A", "Naar", "Do", "В", "Nereye",
          "إلى", "目标", "先", "대상", "को", "Ke",
          "Till", "Kohde", "Do", "ไปยัง");
        A("histResult", "Result", "Resultado", "Resultado", "Résultat", "Ergebnis",
          "Esito", "Resultaat", "Wynik", "Результат", "Sonuç",
          "النتيجة", "结果", "結果", "결과", "परिणाम", "Hasil",
          "Resultat", "Tulos", "Výsledek", "ผลลัพธ์");
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
        A("engineInfo", "SuperCopier-based parallel copy engine - blazing fast",
          "Motor de copia paralela basado en SuperCopier - rapidísimo",
          "Motor de cópia paralela baseado no SuperCopier - rapidíssimo",
          "Moteur de copie parallèle inspiré de SuperCopier - très rapide",
          "SuperCopier-basierter Parallelkopier-Motor - blitzschnell",
          "Motore di copia parallela basato su SuperCopier - velocissimo",
          "Op SuperCopier gebaseerde parallelle kopijmotor - razendsnel",
          "Silnik równoległy oparty na SuperCopier - błyskawiczny",
          "Движок на основе SuperCopier - супербыстрый",
          "SuperCopier tabanlı paralel motor - çok hızlı",
          "محرك نسخ متوازٍ مستند إلى SuperCopier - سريع للغاية",
          "基于 SuperCopier 的并行复制引擎 - 闪电般快速",
          "SuperCopier ベースの並列コピーエンジン - 超高速",
          "SuperCopier 기반 병렬 엔진 - 초고속",
          "SuperCopier-आधारित समानांतर इंजन - बेहद तेज़",
          "Mesin salin paralel berbasis SuperCopier - sangat cepat",
          "SuperCopier-baserad parallellmotor - blixtsnabb",
          "SuperCopier-pohjainen rinnakkaismoottori - salamannopea",
          "Paralelní jádro založené na SuperCopier - bleskurychlé",
          "เอนจินคัดลอกแบบขนานบนพื้นฐาน SuperCopier - เร็วสุด ๆ");
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
          "Вверх", "Fel", "إلى أعلى", "上",
          "上へ", "위로", "ऊपर", "Na atas", "Upp", "Ylös", "Nahoru", "Nahor", "ขึ้น");
        A("nothingHere", "Nothing here yet", "Aquí no hay nada aún", "Aqui não há nada ainda",
          "Rien ici pour l’instant", "Hier ist noch nichts", "Qui non c’è ancora niente",
          "Hier is nog niets", "Tu jeszcze nic tu nie ma", "Здесь пока пусто",
          "Burada henüz yok", "لا شيء هنا بعد", "这里还没有东西",
          "ここにはまだない", "여기 아직 없음", "यहाँ अभी कुछ नहीं", "Ma belum ada di sini",
          "Inget här än", "Ei mitään vielä", "Zatím tu nic", "Zatiaľ tu nič", "ยังไม่มีอะไรที่นี่");
        A("tapToMark", "Tap a file to mark it", "Toca un archivo para marcarlo", "Toque um arquivo para marcar",
          "Touchez un fichier pour le cocher", "Datei antippen zum markieren", "Tocca un file per selezionarlo",
          "Tik op een bestand om het te markeren", "Dotknij plik, aby go zaznaczyć", "Коснитесь файла, чтобы выбрать его",
          "Bir dosyaya dokunun", "المس ملفًا لتحديده", "点按文件以选中",
          "ファイルをタップして選択", "파일을 탭하여 선택", "फ़ाइल चुनने के लिए टैप करें", "المس ملفًا لتحديده",
          "Tryck på en fil för att markera", "Napauta tiedostoa valitaksesi", "Kliknite na soubor pro označení",
          "Kliknite na súbor pre označenie", "แตะไฟล์เพื่อทำเครื่องหมาย");
        A("markedFmt", "{0} marked · {1}", "{0} marcados · {1}", "{0} marcados · {1}",
          "{0} cochés · {1}", "{0} markiert · {1}", "{0} selezionati · {1}",
          "{0} gemarkeerd · {1}", "Zaznaczono: {0} · {1}", "Выбрано: {0} · {1}",
          "{0} işaretli · {1}", "المحدد: {0} · {1}", "已选 {0} 项 · {1}",
          "{0} 選択しました · {1}", "{0}개 선택됨 · {1}", "चिन्हित: {0} · {1}", "المحدد: {0} · {1}",
          "{0} markerade · {1}", "Valittu {0} · {1}", "Valittu {0} · {1}", "Označeno: {0} · {1}", "ทำเครื่องหมาย {0} รายการ · {1}");
        A("addToList", "Add to list", "Añadir a la lista", "Adicionar à lista", "Ajouter à la liste",
          "Zur Liste", "Aggiungi alla lista", "Aan lijst toevoegen", "Dodaj do listy", "Добавить в список",
          "Listeye ekle", "إضافة إلى القائمة", "加入列表",
          "リストに追加", "리스트에 추가", "सूची में जोड़ें", "إضافة إلى القائمة",
          "Lägg till i listan", "Lisää listaan", "Přidat do seznamu", "Pridať do zoznamu", "添加到列表");
        A("useAsDest", "Use as destination", "Usar como destino", "Usar como destino", "Utiliser comme destination",
          "Als Ziel verwenden", "Usa come destinazione", "Als bestemming gebruiken", "Użyj jako miejsce docelowe",
          "Использовать как папку", "Hedef olarak kullan", "استخدم كوجهة", "设为目的地",
          "保存先にする", "대상으로 사용", "गंतव्य के रूप में उपयोग करें", "استخدم كوجهة",
          "Använd som mål", "Käytä kohteena", "Použít jako cíl", "Použiť ako miesto príchodu", "设为目的地");
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
    }
}