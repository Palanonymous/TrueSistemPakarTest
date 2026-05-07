  PSEUDOCODE SISTEM PAKAR DDA
  Dynamic Difficulty Adjustment — Game Boss
  (Menggunakan Forward Chaining + Certainty Factor)


────────────────────────────────────────────────────────────
  BAGIAN 1 — STRUKTUR DATA
────────────────────────────────────────────────────────────

STRUKTUR WorkingMemory:
    jawaban : Dictionary<String, Boolean>   // jawaban Ya/Tidak dari user
    cf      : Dictionary<String, Double>    // nilai keyakinan tiap fakta
    alasan  : Dictionary<String, String>    // alasan kesimpulan difficulty
    log     : List<String>                  // jejak penalaran

STRUKTUR Rule:
    nama       : String
    sudahJalan : Boolean
    kondisi    : Fungsi(WorkingMemory) → Boolean
    aksi       : Prosedur(WorkingMemory)


────────────────────────────────────────────────────────────
  BAGIAN 2 — PROSEDUR & FUNGSI WORKING MEMORY
────────────────────────────────────────────────────────────

PROCEDURE SimpanJawaban(id, nilai)
    jawaban[id] = nilai
END PROCEDURE

───────────────────────────

FUNCTION Ya(id) → Boolean
    RETURN (jawaban mengandung id) DAN (jawaban[id] = True)
END FUNCTION

FUNCTION Tidak(id) → Boolean
    RETURN (jawaban mengandung id) DAN (jawaban[id] = False)
END FUNCTION

───────────────────────────

PROCEDURE SimpanCF(key, cf)
    cf = MAX(0.0, MIN(1.0, cf))            // batasi CF di antara 0.0–1.0
    IF key sudah ada di cf THEN
        lama    = cf[key]
        cf[key] = lama + cf * (1.0 - lama) // kombinasikan CF lama + baru
    ELSE
        cf[key] = cf                        // simpan CF pertama kali
    ENDIF
END PROCEDURE

FUNCTION GetCF(key) → Double
    IF key ada di cf THEN
        RETURN cf[key]
    ELSE
        RETURN 0.0
    ENDIF
END FUNCTION

FUNCTION Ada(key) → Boolean
    RETURN (key ada di cf) DAN (cf[key] > 0)
END FUNCTION

───────────────────────────

PROCEDURE SimpanAlasan(key, teks)
    IF key belum ada di alasan THEN
        alasan[key] = teks
    ENDIF
END PROCEDURE

FUNCTION GetAlasan(key) → String
    IF key ada di alasan THEN
        RETURN alasan[key]
    ELSE
        RETURN "-"
    ENDIF
END FUNCTION


────────────────────────────────────────────────────────────
  BAGIAN 3 — FUNGSI KOMBINASI CERTAINTY FACTOR
────────────────────────────────────────────────────────────

FUNCTION CF(a, b) → Double
    IF a <= 0 THEN RETURN b
    IF b <= 0 THEN RETURN a
    RETURN ROUND(a + b * (1.0 - a), 4)    // rumus kombinasi CF paralel
END FUNCTION


────────────────────────────────────────────────────────────
  BAGIAN 4 — FUNGSI TANYA USER
────────────────────────────────────────────────────────────

FUNCTION TanyaUser(nomor, pertanyaan) → Boolean ATAU Null
    OUTPUT ("[" + nomor + "/4] " + pertanyaan)
    OUTPUT ("  [y = Ya  /  n = Tidak  /  Enter = tidak tahu]: ")
    INPUT input

    IF input = "y" ATAU input = "ya"     THEN RETURN True
    IF input = "n" ATAU input = "tidak"  THEN RETURN False
    RETURN Null                          // jika Enter → tidak tahu
END FUNCTION


────────────────────────────────────────────────────────────
  BAGIAN 5 — PROSEDUR JALANKAN GRUP RULE (Forward Chaining)
────────────────────────────────────────────────────────────

PROCEDURE JalankanGrup(wm, isGrup1)
    adaYangJalan = True

    WHILE adaYangJalan = True DO
        adaYangJalan = False

        FOR EACH rule dalam _rules DO

            IF rule.sudahJalan = True THEN
                CONTINUE                  // lewati rule yang sudah aktif
            ENDIF

            // cek apakah ini rule difficulty (Grup 2)
            iniGrup2 = (rule.nama mengandung "LEGENDARY"
                        ATAU "HARD" ATAU "NORMAL" ATAU "EASY")

            IF isGrup1 = True  DAN iniGrup2 = True  THEN CONTINUE
            IF isGrup1 = False DAN iniGrup2 = False THEN CONTINUE

            IF rule.KondisiTerpenuhi(wm) = True THEN
                rule.Jalankan(wm)         // tembakkan aksi rule
                rule.sudahJalan = True
                adaYangJalan    = True
                wm.log.Tambah("  Rule aktif: " + rule.nama)
            ENDIF

        ENDFOR

    ENDWHILE                              // ulangi sampai tidak ada rule baru
END PROCEDURE


────────────────────────────────────────────────────────────
  BAGIAN 6 — PROSEDUR FALLBACK (jika rule difficulty tidak aktif)
────────────────────────────────────────────────────────────

PROCEDURE InferFallback(wm)
    adaDifficulty = (ada key diawali "Difficulty:" di wm.cf)

    IF adaDifficulty = True THEN
        RETURN                            // sudah ada hasil, tidak perlu fallback
    ENDIF

    wm.log.Tambah("[Fallback] Data parsial — estimasi dari indikator awal...")

    IF wm.Ada("SurvivalBagus") DAN wm.Ada("SkillTinggi") THEN
        cf = CF(wm.GetCF("SurvivalBagus"), wm.GetCF("SkillTinggi"))
        wm.SimpanCF("Difficulty:Hard", ROUND(cf * 0.78, 2))
        wm.SimpanAlasan("Difficulty:Hard",
            "Clear tanpa mati dan cepat (estimasi dari data belum lengkap)")
        wm.log.Tambah("Fallback: HARD — SurvivalBagus + SkillTinggi")

    ELSE IF wm.Ada("SurvivalBagus") DAN wm.Ada("SkillSedang") THEN
        cf = CF(wm.GetCF("SurvivalBagus"), wm.GetCF("SkillSedang"))
        wm.SimpanCF("Difficulty:Normal", ROUND(cf * 0.65, 2))
        wm.SimpanAlasan("Difficulty:Normal",
            "Clear tanpa mati namun lambat (estimasi dari data belum lengkap)")
        wm.log.Tambah("Fallback: NORMAL — SurvivalBagus + SkillSedang")

    ELSE IF wm.Ada("SurvivalBuruk") THEN
        cf = wm.GetCF("SurvivalBuruk")
        wm.SimpanCF("Difficulty:Easy", ROUND(cf * 0.80, 2))
        wm.SimpanAlasan("Difficulty:Easy",
            "Player mati lebih dari 3 kali (estimasi dari data belum lengkap)")
        wm.log.Tambah("Fallback: EASY — SurvivalBuruk")

    ELSE IF wm.Ada("SurvivalCukup") THEN
        cf = wm.GetCF("SurvivalCukup")
        wm.SimpanCF("Difficulty:Normal", ROUND(cf * 0.60, 2))
        wm.SimpanAlasan("Difficulty:Normal",
            "Player mati 1-3 kali (estimasi dari data belum lengkap)")
        wm.log.Tambah("Fallback: NORMAL — SurvivalCukup")

    ELSE IF wm.Ada("SurvivalBagus") THEN
        cf = wm.GetCF("SurvivalBagus")
        wm.SimpanCF("Difficulty:Normal", ROUND(cf * 0.55, 2))
        wm.SimpanAlasan("Difficulty:Normal",
            "Hanya diketahui player clear tanpa mati (data sangat terbatas)")
        wm.log.Tambah("Fallback: NORMAL — hanya SurvivalBagus")

    ELSE IF wm.Tidak("Q1") THEN
        wm.SimpanCF("Difficulty:Easy", 0.55)
        wm.SimpanAlasan("Difficulty:Easy",
            "Player mati setidaknya sekali — perkiraan awal Easy")
        wm.log.Tambah("Fallback: EASY — Q1=Tidak, data tidak lengkap")
    ENDIF

END PROCEDURE


────────────────────────────────────────────────────────────
  BAGIAN 7 — PROSEDUR MESIN INFERENSI (UTAMA)
────────────────────────────────────────────────────────────

PROCEDURE Jalankan(wm)
    soal  = "Q1"
    nomor = 1

    WHILE soal ≠ Null DO

        jawab = TanyaUser(nomor, Pertanyaan[soal])

        IF jawab = Null THEN
            wm.log.Tambah("[" + nomor + "/4] " + soal
                          + " = Tidak Tahu → lanjut ke hasil")
            BREAK                             // hentikan pertanyaan
        ENDIF

        wm.SimpanJawaban(soal, jawab)

        IF jawab = True THEN
            wm.log.Tambah("[" + nomor + "/4] " + soal + " = Ya")
        ELSE
            wm.log.Tambah("[" + nomor + "/4] " + soal + " = Tidak")
        ENDIF

        JalankanGrup(wm, isGrup1 = True)     // tembakkan rule indikator

        // tentukan pertanyaan berikutnya dari pohon soal
        IF jawab = True THEN
            keyPohon = soal + "|Y"
        ELSE
            keyPohon = soal + "|N"
        ENDIF

        soal  = PohonSoal[keyPohon]          // null jika tidak ada cabang
        nomor = nomor + 1

    ENDWHILE

    wm.log.Tambah("--- Menentukan difficulty ---")
    JalankanGrup(wm, isGrup1 = False)        // tembakkan rule difficulty
    InferFallback(wm)                        // fallback jika belum ada hasil

END PROCEDURE


────────────────────────────────────────────────────────────
  BAGIAN 8 — PROSEDUR TAMPILKAN HASIL
────────────────────────────────────────────────────────────

PROCEDURE TampilkanHasil(wm)
    diff  = Null
    cfMax = 0.0

    // cari difficulty dengan CF tertinggi
    FOR EACH f dalam wm.SemuaCF() DO
        IF f.Key TIDAK diawali "Difficulty:" THEN
            CONTINUE
        ENDIF
        IF f.Value > cfMax THEN
            cfMax = f.Value
            diff  = f.Key tanpa awalan "Difficulty:"
        ENDIF
    ENDFOR

    OUTPUT ("================================================")
    OUTPUT ("  HASIL DIAGNOSIS DIFFICULTY")
    OUTPUT ("================================================")

    IF diff = Null THEN
        OUTPUT ("  Belum cukup informasi untuk menentukan difficulty.")
        OUTPUT ("  Coba jawab minimal 1 pertanyaan.")
    ELSE
        IF cfMax >= 0.85 THEN
            yakin = "Sangat Yakin"
        ELSE IF cfMax >= 0.65 THEN
            yakin = "Cukup Yakin"
        ELSE
            yakin = "Perkiraan"
        ENDIF

        OUTPUT ("  Difficulty : " + diff)
        OUTPUT ("  Boss       : " + NamaBoss(diff))
        OUTPUT ("  Alasan Boss: " + AlasanBoss(diff))
        OUTPUT ("  Keyakinan  : " + yakin + " (" + cfMax*100 + "%)")
        OUTPUT ("  Keterangan : " + Keterangan(diff))
        OUTPUT ("  Alasan     : " + wm.GetAlasan("Difficulty:" + diff))
    ENDIF

    // tampilkan semua indikator yang terdeteksi
    OUTPUT ("  Indikator yang Terdeteksi:")
    FOR EACH f dalam wm.SemuaCF() DO
        IF f.Key TIDAK diawali "Difficulty:" THEN
            OUTPUT ("    " + f.Key + " : " + f.Value*100 + "%")
        ENDIF
    ENDFOR

    // tampilkan jejak penalaran
    OUTPUT ("================================================")
    OUTPUT ("  JEJAK PENALARAN (Rule yang Aktif)")
    OUTPUT ("================================================")
    FOR EACH baris dalam wm.log DO
        OUTPUT ("  " + baris)
    ENDFOR

END PROCEDURE

───────────────────────────

FUNCTION NamaBoss(d) → String
    IF d = "Legendary" THEN RETURN "Naga Kuno (Boss Tertinggi)"
    IF d = "Hard"      THEN RETURN "Naga"
    IF d = "Normal"    THEN RETURN "Golem"
    IF d = "Easy"      THEN RETURN "Goblin"
    RETURN d
END FUNCTION

FUNCTION AlasanBoss(d) → String
    IF d = "Legendary" THEN RETURN "Boss tertinggi karena performa sempurna di semua kategori"
    IF d = "Hard"      THEN RETURN "Boss kuat karena player sudah berpengalaman dan efisien"
    IF d = "Normal"    THEN RETURN "Boss standar karena performa seimbang"
    IF d = "Easy"      THEN RETURN "Boss ringan agar player bisa belajar tanpa frustrasi"
    RETURN "-"
END FUNCTION

FUNCTION Keterangan(d) → String
    IF d = "Legendary" THEN RETURN "Player sangat mahir! Tantangan tertinggi menanti."
    IF d = "Hard"      THEN RETURN "Player berpengalaman. Boss kuat untuk tantangan optimal."
    IF d = "Normal"    THEN RETURN "Performa seimbang. Boss standar yang sesuai."
    IF d = "Easy"      THEN RETURN "Player masih berkembang. Boss ringan untuk membangun skill."
    RETURN "-"
END FUNCTION


────────────────────────────────────────────────────────────
  BAGIAN 9 — PROGRAM UTAMA
────────────────────────────────────────────────────────────

Begin
    OUTPUT ("================================================")
    OUTPUT ("  SISTEM PAKAR DDA")
    OUTPUT ("  Dynamic Difficulty Adjustment — Game Boss")
    OUTPUT ("================================================")
    OUTPUT ("  y = Ya  |  n = Tidak  |  Enter = tidak tahu")
    OUTPUT ("------------------------------------------------")

    wm     = WorkingMemory baru
    rules  = KnowledgeBase.BuatSemuaRule()
    engine = InferenceEngine baru dengan rules

    engine.Jalankan(wm)
    TampilkanHasil(wm)

End

============================================================
  CATATAN ALUR FORWARD CHAINING
============================================================

  Alur inferensi sistem ini:

  [1] Mulai dari Q1 (fakta awal dari user)
       ↓
  [2] Simpan jawaban ke WorkingMemory
       ↓
  [3] Tembakkan Grup 1 → hasilkan indikator intermediate
      (SurvivalBagus, SkillTinggi, ResourceSempurna, dll.)
       ↓
  [4] Ikuti cabang PohonSoal → tanya pertanyaan berikutnya
       ↓
  [5] Ulangi [2]–[4] hingga pertanyaan habis atau user tidak tahu
       ↓
  [6] Tembakkan Grup 2 → tentukan Difficulty:Legendary/Hard/Normal/Easy
       ↓
  [7] Jika Grup 2 tidak menghasilkan → jalankan Fallback
       ↓
  [8] Tampilkan hasil: Difficulty + Boss + CF + Jejak Penalaran

============================================================
