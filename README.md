# 🎮 Expert System Dungeon (Forward Chaining + Certainty Factor)

Sistem Pakar berbasis konsol (Console Application) yang dibangun menggunakan bahasa pemrograman **C# (.NET)**. Proyek ini dirancang untuk menganalisis performa pemain di dalam permainan RPG/Dungeon berdasarkan beberapa indikator klinis (fakta), menentukan klasifikasi tingkat kesulitan dungeon yang cocok, serta menghitung nilai kepastian (*Certainty Factor*) menggunakan metode **MYCIN**.

---

## 🚀 Fitur Utama

* **Forward Chaining (Fase 1-3):** Sistem mengumpulkan fakta input dari 3 percobaan dungeon (Working Memory), menerapkan aturan inferensi untuk menentukan status kemenangan (`Win/Lose`), lalu memetakan skor performa ke dalam kategori (`High`, `Medium`, `Low`).
* **Certainty Factor (MYCIN Berlapis):** Menggabungkan beberapa *evidence* (bukti kepastian) secara iteratif dari 3 dungeon untuk menghasilkan satu nilai keyakinan akhir ($CF_{combined}$) menggunakan rumus:
    $$CF_{combined} = CF_{prev} + CF_{new} \times (1 - CF_{prev})$$
* **Logika Invers Berbasis Desain:** Skor HP dan Level dirancang terbalik (*inverse by design*). Pemain yang menyelesaikan dungeon dengan Level rendah atau HP awal yang kritis dianggap memiliki performa yang jauh lebih impresif ($100\%$).
* **Sistem Validasi Input:** Menjamin data yang dimasukkan oleh pengguna berada dalam rentang parameter yang valid sebelum diproses oleh mesin inferensi.

---

## 📊 Alur Logika & Aturan Sistem Pakar

### 1. Aturan Penentu Kemenangan (Rule R1)
* **IF** `Death > 6` **OR** `ClearTime > 30 Menit` **THEN** `Win = False` (Lose)
* **ELSE** `Win = True` (Win)

### 2. Aturan Klasifikasi Performa per Dungeon
Sistem menghitung rata-rata dari 5 indikator (HP, Potion, Waktu, Kematian, Level) dengan bobot seimbang (*equal weight* @20%):
* **Rule R2:** `PerformancePercent >= 80` $\rightarrow$ Kategori **High** ($CF = 0.80$)
* **Rule R3:** `PerformancePercent >= 60` $\rightarrow$ Kategori **Medium** ($CF = 0.60$)
* **Rule R4:** `PerformancePercent < 60` $\rightarrow$ Kategori **Low** ($CF = 0.40$)

### 3. Majority Rule Penentu Kesulitan Akhir (Final Decision)
* **Rule R5 (HARD):** Mayoritas kategori (2 atau lebih) bernilai **High** $\rightarrow$ Rekomendasi: `HARD (Dragon)`
* **Rule R6 (NORMAL):** Mayoritas kategori (2 atau lebih) bernilai **Medium** $\rightarrow$ Rekomendasi: `NORMAL (Golem)`
* **Rule R7 (Tie-Breaker):** Jika distribusi seimbang (1 High, 1 Medium, 1 Low) $\rightarrow$ Rekomendasi: `NORMAL (Golem)`
* **Rule R8 (EASY):** Jika tidak memenuhi kondisi di atas (mayoritas Low) $\rightarrow$ Rekomendasi: `EASY (Goblin)`

---

## 🛠️ Spesifikasi Input (Working Memory)

| Parameter Input | Rentang Valid | Keterangan Logika |
| :--- | :---: | :--- |
| **Player Level** | 1 - 10 | Level lebih rendah $\rightarrow$ Skor Performa makin tinggi |
| **Starting HP** | 1 - 100 | HP awal lebih kecil $\rightarrow$ Skor Performa makin tinggi |
| **Potion Used** | 0 - 5 | Penggunaan Potion sedikit $\rightarrow$ Skor Performa makin tinggi |
| **Clear Time** | 0 - 60 mnt | Di bawah 10 menit ideal ($100\%$), di atas 30 menit otomatis *Lose* |
| **Jumlah Respawn** | 0 - 20 kali | Tidak pernah mati ideal ($100\%$), di atas 6 kali otomatis *Lose* |

## 📂 Struktur Repositori

```text
TrueSistemPakarTest/
│
├── TrueSistemPakarTest/
│   ├── Program.cs          # Berkas utama yang berisi seluruh logika kode Sistem Pakar
│   └── TrueSistemPakarTest.csproj
└── TrueSistemPakarTest.sln # Solusi proyek Visual Studio
