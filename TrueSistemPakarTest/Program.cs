using System;
using System.Collections.Generic;
using System.Linq;

namespace SistemPakarDDA
{

    class WorkingMemory
    {
        private Dictionary<string, bool> _jawaban = new Dictionary<string, bool>();
        private Dictionary<string, double> _cf = new Dictionary<string, double>();
        private Dictionary<string, string> _alasan = new Dictionary<string, string>();
        public List<string> Log = new List<string>();

        public void SimpanJawaban(string id, bool val)
        {
            _jawaban[id] = val;
        }

        public bool Ya(string id)
        {
            return _jawaban.ContainsKey(id) && _jawaban[id];
        }

        public bool Tidak(string id)
        {
            return _jawaban.ContainsKey(id) && !_jawaban[id];
        }

        public void SimpanCF(string key, double cf)
        {
            cf = Math.Max(0.0, Math.Min(1.0, cf));

            if (_cf.ContainsKey(key))
            {
                double lama = _cf[key];
                _cf[key] = lama + cf * (1.0 - lama);
            }
            else
            {
                _cf[key] = cf;
            }
        }

        public double GetCF(string key)
        {
            return _cf.ContainsKey(key) ? _cf[key] : 0.0;
        }

        public bool Ada(string key)
        {
            return _cf.ContainsKey(key) && _cf[key] > 0;
        }

        public Dictionary<string, double> SemuaCF()
        {
            return _cf;
        }

        public void SimpanAlasan(string key, string alasan)
        {
            if (!_alasan.ContainsKey(key))
                _alasan[key] = alasan;
        }

        public string GetAlasan(string key)
        {
            return _alasan.ContainsKey(key) ? _alasan[key] : "-";
        }
    }

    class Rule
    {
        public string Nama { get; private set; }
        public bool SudahJalan { get; set; }

        private Func<WorkingMemory, bool> _kondisi;
        private Action<WorkingMemory> _aksi;

        public Rule(string nama, Func<WorkingMemory, bool> kondisi, Action<WorkingMemory> aksi)
        {
            Nama = nama;
            SudahJalan = false;
            _kondisi = kondisi;
            _aksi = aksi;
        }

        public bool KondisiTerpenuhi(WorkingMemory wm)
        {
            return _kondisi(wm);
        }

        public void Jalankan(WorkingMemory wm)
        {
            _aksi(wm);
        }
    }

    static class KnowledgeBase
    {
        public static Dictionary<string, string> Pertanyaan =
            new Dictionary<string, string>()
        {
            { "Q1",  "Player clear dungeon tanpa mati sama sekali?"           },
            { "Q2A", "Dungeon selesai dalam waktu kurang dari 10 menit?"      },
            { "Q2B", "Player mati lebih dari 3 kali?"                         },
            { "Q3A", "HP yang tersisa lebih dari 70%?"                        },
            { "Q3B", "HP yang tersisa lebih dari 50%?"                        },
            { "Q3C", "Level karakter di bawah level 5?"                       },
            { "Q3D", "Player menggunakan lebih dari 3 potion?"                },
            { "Q4A", "Potion yang dipakai kurang dari 2?"                     },
            { "Q4B", "Level karakter level 8 ke atas?"                        },
            { "Q4C", "Player menggunakan lebih dari 3 potion selama dungeon?" },
            { "Q4D", "Level karakter di atas level 6?"                        },
            { "Q4E", "Ini dungeon pertama kali yang dimainkan player?"        },
            { "Q4F", "Player sering terkena serangan boss?"                   },
            { "Q4G", "HP yang tersisa kurang dari 30%?"                       },
            { "Q4H", "Waktu clear dungeon antara 15 sampai 20 menit?"         },
        };

        public static Dictionary<string, string> PohonSoal =
            new Dictionary<string, string>()
        {
            { "Q1|Y",  "Q2A" }, { "Q1|N",  "Q2B" },
            { "Q2A|Y", "Q3A" }, { "Q2A|N", "Q3B" },
            { "Q2B|Y", "Q3C" }, { "Q2B|N", "Q3D" },
            { "Q3A|Y", "Q4A" }, { "Q3A|N", "Q4B" },
            { "Q3B|Y", "Q4C" }, { "Q3B|N", "Q4D" },
            { "Q3C|Y", "Q4E" }, { "Q3C|N", "Q4F" },
            { "Q3D|Y", "Q4G" }, { "Q3D|N", "Q4H" },
        };

        public static double CF(double a, double b)
        {
            if (a <= 0) return b;
            if (b <= 0) return a;
            return Math.Round(a + b * (1.0 - a), 4);
        }

        public static List<Rule> BuatSemuaRule()
        {
            var R = new List<Rule>();

            R.Add(new Rule(
                "Tidak mati → survival bagus",
                wm => wm.Ya("Q1"),
                wm => wm.SimpanCF("SurvivalBagus", 0.95)
            ));

            R.Add(new Rule(
                "Tidak mati + cepat → skill tinggi",
                wm => wm.Ya("Q1") && wm.Ya("Q2A"),
                wm => wm.SimpanCF("SkillTinggi", 0.93)
            ));

            R.Add(new Rule(
                "Tidak mati + lambat → skill sedang",
                wm => wm.Ya("Q1") && wm.Tidak("Q2A"),
                wm => wm.SimpanCF("SkillSedang", 0.75)
            ));

            R.Add(new Rule(
                "Mati lebih dari 3x → survival buruk",
                wm => wm.Tidak("Q1") && wm.Ya("Q2B"),
                wm => wm.SimpanCF("SurvivalBuruk", 0.92)
            ));

            R.Add(new Rule(
                "Mati 1-3x → survival cukup",
                wm => wm.Tidak("Q1") && wm.Tidak("Q2B"),
                wm => wm.SimpanCF("SurvivalCukup", 0.70)
            ));

            R.Add(new Rule(
                "HP lebih 70% + tidak mati + cepat → resource sempurna",
                wm => wm.Ya("Q1") && wm.Ya("Q2A") && wm.Ya("Q3A"),
                wm => wm.SimpanCF("ResourceSempurna", 0.92)
            ));

            R.Add(new Rule(
                "HP kurang 70% + tidak mati + cepat → resource baik",
                wm => wm.Ya("Q1") && wm.Ya("Q2A") && wm.Tidak("Q3A"),
                wm => wm.SimpanCF("ResourceBaik", 0.78)
            ));

            R.Add(new Rule(
                "HP lebih 50% + lambat → resource cukup",
                wm => wm.Ya("Q1") && wm.Tidak("Q2A") && wm.Ya("Q3B"),
                wm => wm.SimpanCF("ResourceCukup", 0.72)
            ));

            R.Add(new Rule(
                "HP kurang 50% + lambat → resource buruk",
                wm => wm.Ya("Q1") && wm.Tidak("Q2A") && wm.Tidak("Q3B"),
                wm => wm.SimpanCF("ResourceBuruk", 0.80)
            ));

            R.Add(new Rule(
                "Level kurang dari 5 + mati banyak → underleveled",
                wm => wm.Tidak("Q1") && wm.Ya("Q2B") && wm.Ya("Q3C"),
                wm => wm.SimpanCF("Underleveled", 0.90)
            ));

            R.Add(new Rule(
                "Level 5 ke atas + mati banyak → skill kurang",
                wm => wm.Tidak("Q1") && wm.Ya("Q2B") && wm.Tidak("Q3C"),
                wm => wm.SimpanCF("SkillKurang", 0.85)
            ));

            R.Add(new Rule(
                "Boros potion + mati sedikit → resource buruk",
                wm => wm.Tidak("Q1") && wm.Tidak("Q2B") && wm.Ya("Q3D"),
                wm => wm.SimpanCF("ResourceBuruk", 0.82)
            ));

            R.Add(new Rule(
                "Hemat potion + mati sedikit → resource baik",
                wm => wm.Tidak("Q1") && wm.Tidak("Q2B") && wm.Tidak("Q3D"),
                wm => wm.SimpanCF("ResourceBaik", 0.80)
            ));

            R.Add(new Rule(
                "Potion kurang dari 2 → efisiensi sempurna",
                wm => wm.Ya("Q1") && wm.Ya("Q2A") && wm.Ya("Q3A") && wm.Ya("Q4A"),
                wm => wm.SimpanCF("EfisiensiSempurna", 0.95)
            ));

            R.Add(new Rule(
                "Potion 2-3 → efisiensi baik",
                wm => wm.Ya("Q1") && wm.Ya("Q2A") && wm.Ya("Q3A") && wm.Tidak("Q4A"),
                wm => wm.SimpanCF("EfisiensiBaik", 0.82)
            ));

            R.Add(new Rule(
                "Level 8 ke atas → pengalaman tinggi",
                wm => wm.Ya("Q1") && wm.Ya("Q2A") && wm.Tidak("Q3A") && wm.Ya("Q4B"),
                wm => wm.SimpanCF("PengalamanTinggi", 0.88)
            ));

            R.Add(new Rule(
                "Level di bawah 8 → pengalaman sedang",
                wm => wm.Ya("Q1") && wm.Ya("Q2A") && wm.Tidak("Q3A") && wm.Tidak("Q4B"),
                wm => wm.SimpanCF("PengalamanSedang", 0.65)
            ));

            R.Add(new Rule(
                "Boros potion + HP cukup → tidak efisien",
                wm => wm.Ya("Q1") && wm.Tidak("Q2A") && wm.Ya("Q3B") && wm.Ya("Q4C"),
                wm => wm.SimpanCF("TidakEfisien", 0.78)
            ));

            R.Add(new Rule(
                "Hemat potion + HP cukup → efisiensi baik",
                wm => wm.Ya("Q1") && wm.Tidak("Q2A") && wm.Ya("Q3B") && wm.Tidak("Q4C"),
                wm => wm.SimpanCF("EfisiensiBaik", 0.80)
            ));

            R.Add(new Rule(
                "Level di atas 6 + lambat → pengalaman cukup",
                wm => wm.Ya("Q1") && wm.Tidak("Q2A") && wm.Tidak("Q3B") && wm.Ya("Q4D"),
                wm => wm.SimpanCF("PengalamanCukup", 0.75)
            ));

            R.Add(new Rule(
                "Level 6 ke bawah + lambat → pengalaman kurang",
                wm => wm.Ya("Q1") && wm.Tidak("Q2A") && wm.Tidak("Q3B") && wm.Tidak("Q4D"),
                wm => wm.SimpanCF("PengalamanKurang", 0.80)
            ));

            R.Add(new Rule(
                "Dungeon pertama + underleveled → newbie",
                wm => wm.Tidak("Q1") && wm.Ya("Q2B") && wm.Ya("Q3C") && wm.Ya("Q4E"),
                wm => wm.SimpanCF("Newbie", 0.95)
            ));

            R.Add(new Rule(
                "Bukan pertama + underleveled → perlu naik level",
                wm => wm.Tidak("Q1") && wm.Ya("Q2B") && wm.Ya("Q3C") && wm.Tidak("Q4E"),
                wm => wm.SimpanCF("PerluNaikLevel", 0.85)
            ));

            R.Add(new Rule(
                "Sering kena hit + mati banyak → skill sangat kurang",
                wm => wm.Tidak("Q1") && wm.Ya("Q2B") && wm.Tidak("Q3C") && wm.Ya("Q4F"),
                wm => wm.SimpanCF("SkillSangatKurang", 0.88)
            ));

            R.Add(new Rule(
                "Jarang kena hit + mati banyak → sedikit kurang",
                wm => wm.Tidak("Q1") && wm.Ya("Q2B") && wm.Tidak("Q3C") && wm.Tidak("Q4F"),
                wm => wm.SimpanCF("SedikitKurang", 0.65)
            ));

            R.Add(new Rule(
                "HP kurang 30% + boros potion → hampir kalah",
                wm => wm.Tidak("Q1") && wm.Tidak("Q2B") && wm.Ya("Q3D") && wm.Ya("Q4G"),
                wm => wm.SimpanCF("HampirKalah", 0.85)
            ));

            R.Add(new Rule(
                "HP 30% ke atas + boros potion → cukup bertahan",
                wm => wm.Tidak("Q1") && wm.Tidak("Q2B") && wm.Ya("Q3D") && wm.Tidak("Q4G"),
                wm => wm.SimpanCF("CukupBertahan", 0.70)
            ));

            R.Add(new Rule(
                "Clear 15-20 menit → performa sedang",
                wm => wm.Tidak("Q1") && wm.Tidak("Q2B") && wm.Tidak("Q3D") && wm.Ya("Q4H"),
                wm => wm.SimpanCF("PerformaSedang", 0.72)
            ));

            R.Add(new Rule(
                "Clear cepat + hemat potion → performa baik",
                wm => wm.Tidak("Q1") && wm.Tidak("Q2B") && wm.Tidak("Q3D") && wm.Tidak("Q4H"),
                wm => wm.SimpanCF("PerformaBaik", 0.82)
            ));

            R.Add(new Rule(
                "LEGENDARY: semua aspek performa sempurna",
                wm => wm.Ada("SurvivalBagus")
                   && wm.Ada("SkillTinggi")
                   && wm.Ada("ResourceSempurna")
                   && wm.Ada("EfisiensiSempurna"),
                wm =>
                {
                    double cf = CF(wm.GetCF("SurvivalBagus"), wm.GetCF("SkillTinggi"));
                    cf = CF(cf, wm.GetCF("ResourceSempurna"));
                    cf = CF(cf, wm.GetCF("EfisiensiSempurna"));
                    wm.SimpanCF("Difficulty:Legendary", Math.Round(cf * 0.97, 2));
                    wm.SimpanAlasan("Difficulty:Legendary",
                        "Clear tanpa mati, sangat cepat, HP tinggi, dan hemat potion — sempurna di semua aspek");
                }
            ));

            R.Add(new Rule(
                "HARD: skill tinggi + survival bagus",
                wm => wm.Ada("SurvivalBagus")
                   && wm.Ada("SkillTinggi")
                   && (wm.Ada("EfisiensiBaik")
                       || wm.Ada("PengalamanTinggi")
                       || wm.Ada("PengalamanSedang")),
                wm =>
                {
                    double cf = CF(wm.GetCF("SurvivalBagus"), wm.GetCF("SkillTinggi"));
                    if (wm.Ada("ResourceSempurna")) cf = CF(cf, wm.GetCF("ResourceSempurna"));
                    if (wm.Ada("ResourceBaik")) cf = CF(cf, wm.GetCF("ResourceBaik"));
                    if (wm.Ada("EfisiensiBaik")) cf = CF(cf, wm.GetCF("EfisiensiBaik"));
                    if (wm.Ada("PengalamanTinggi")) cf = CF(cf, wm.GetCF("PengalamanTinggi"));
                    if (wm.Ada("PengalamanSedang")) cf = CF(cf, wm.GetCF("PengalamanSedang"));
                    wm.SimpanCF("Difficulty:Hard", Math.Round(cf * 0.90, 2));
                    wm.SimpanAlasan("Difficulty:Hard",
                        "Clear tanpa mati dengan skill dan kecepatan tinggi");
                }
            ));

            R.Add(new Rule(
                "HARD: hemat potion + skill sedang",
                wm => wm.Ada("SurvivalBagus") && wm.Ada("SkillSedang") && wm.Ada("EfisiensiBaik"),
                wm =>
                {
                    double cf = CF(wm.GetCF("SurvivalBagus"), wm.GetCF("SkillSedang"));
                    cf = CF(cf, wm.GetCF("EfisiensiBaik"));
                    if (wm.Ada("ResourceCukup")) cf = CF(cf, wm.GetCF("ResourceCukup"));
                    wm.SimpanCF("Difficulty:Hard", Math.Round(cf * 0.85, 2));
                    wm.SimpanAlasan("Difficulty:Hard",
                        "Clear tanpa mati, lambat tapi efisien dalam penggunaan potion");
                }
            ));

            R.Add(new Rule(
                "HARD: mati sedikit + performa baik",
                wm => wm.Ada("SurvivalCukup") && wm.Ada("ResourceBaik") && wm.Ada("PerformaBaik"),
                wm =>
                {
                    double cf = CF(wm.GetCF("SurvivalCukup"), wm.GetCF("ResourceBaik"));
                    cf = CF(cf, wm.GetCF("PerformaBaik"));
                    wm.SimpanCF("Difficulty:Hard", Math.Round(cf * 0.83, 2));
                    wm.SimpanAlasan("Difficulty:Hard",
                        "Mati 1-3 kali namun resource baik dan clear cepat");
                }
            ));

            R.Add(new Rule(
                "NORMAL: skill sedang + boros potion",
                wm => wm.Ada("SurvivalBagus") && wm.Ada("SkillSedang") && wm.Ada("TidakEfisien"),
                wm =>
                {
                    double cf = CF(wm.GetCF("SurvivalBagus"), wm.GetCF("SkillSedang"));
                    cf = CF(cf, wm.GetCF("TidakEfisien"));
                    if (wm.Ada("ResourceCukup")) cf = CF(cf, wm.GetCF("ResourceCukup"));
                    wm.SimpanCF("Difficulty:Normal", Math.Round(cf * 0.75, 2));
                    wm.SimpanAlasan("Difficulty:Normal",
                        "Clear tanpa mati tapi lambat dan boros potion");
                }
            ));

            R.Add(new Rule(
                "NORMAL: pengalaman cukup + resource buruk",
                wm => wm.Ada("SurvivalBagus") && wm.Ada("SkillSedang") && wm.Ada("PengalamanCukup"),
                wm =>
                {
                    double cf = CF(wm.GetCF("SurvivalBagus"), wm.GetCF("SkillSedang"));
                    cf = CF(cf, wm.GetCF("PengalamanCukup"));
                    if (wm.Ada("ResourceBuruk")) cf = CF(cf, wm.GetCF("ResourceBuruk"));
                    wm.SimpanCF("Difficulty:Normal", Math.Round(cf * 0.72, 2));
                    wm.SimpanAlasan("Difficulty:Normal",
                        "Clear tanpa mati, lambat, pengalaman dan resource terbatas");
                }
            ));

            R.Add(new Rule(
                "NORMAL: mati sedikit + boros potion",
                wm => wm.Ada("SurvivalCukup") && wm.Ada("CukupBertahan"),
                wm =>
                {
                    double cf = CF(wm.GetCF("SurvivalCukup"), wm.GetCF("CukupBertahan"));
                    if (wm.Ada("ResourceBuruk")) cf = CF(cf, wm.GetCF("ResourceBuruk"));
                    wm.SimpanCF("Difficulty:Normal", Math.Round(cf * 0.70, 2));
                    wm.SimpanAlasan("Difficulty:Normal",
                        "Mati 1-3 kali dan boros potion dalam dungeon");
                }
            ));

            R.Add(new Rule(
                "NORMAL: mati sedikit + clear lambat",
                wm => wm.Ada("SurvivalCukup") && wm.Ada("PerformaSedang"),
                wm =>
                {
                    double cf = CF(wm.GetCF("SurvivalCukup"), wm.GetCF("PerformaSedang"));
                    if (wm.Ada("ResourceBaik")) cf = CF(cf, wm.GetCF("ResourceBaik"));
                    wm.SimpanCF("Difficulty:Normal", Math.Round(cf * 0.68, 2));
                    wm.SimpanAlasan("Difficulty:Normal",
                        "Mati 1-3 kali dan waktu clear lambat");
                }
            ));

            R.Add(new Rule(
                "NORMAL: mati banyak tapi jarang kena hit",
                wm => wm.Ada("SurvivalBuruk") && wm.Ada("SedikitKurang"),
                wm =>
                {
                    double cf = CF(wm.GetCF("SurvivalBuruk"), wm.GetCF("SedikitKurang"));
                    if (wm.Ada("SkillKurang")) cf = CF(cf, wm.GetCF("SkillKurang"));
                    wm.SimpanCF("Difficulty:Normal", Math.Round(cf * 0.65, 2));
                    wm.SimpanAlasan("Difficulty:Normal",
                        "Mati banyak namun jarang terkena serangan — skill masih berkembang");
                }
            ));

            R.Add(new Rule(
                "EASY: newbie total, dungeon pertama",
                wm => wm.Ada("Newbie"),
                wm =>
                {
                    double cf = wm.GetCF("Newbie");
                    if (wm.Ada("SurvivalBuruk")) cf = CF(cf, wm.GetCF("SurvivalBuruk"));
                    if (wm.Ada("Underleveled")) cf = CF(cf, wm.GetCF("Underleveled"));
                    wm.SimpanCF("Difficulty:Easy", Math.Round(cf * 0.95, 2));
                    wm.SimpanAlasan("Difficulty:Easy",
                        "Dungeon pertama dan underleveled — player baru mulai bermain");
                }
            ));

            R.Add(new Rule(
                "EASY: underleveled, perlu naik level dulu",
                wm => wm.Ada("PerluNaikLevel"),
                wm =>
                {
                    double cf = wm.GetCF("PerluNaikLevel");
                    if (wm.Ada("SurvivalBuruk")) cf = CF(cf, wm.GetCF("SurvivalBuruk"));
                    if (wm.Ada("Underleveled")) cf = CF(cf, wm.GetCF("Underleveled"));
                    wm.SimpanCF("Difficulty:Easy", Math.Round(cf * 0.90, 2));
                    wm.SimpanAlasan("Difficulty:Easy",
                        "Underleveled dan bukan dungeon pertama — perlu grinding level");
                }
            ));

            R.Add(new Rule(
                "EASY: skill sangat kurang",
                wm => wm.Ada("SkillSangatKurang"),
                wm =>
                {
                    double cf = wm.GetCF("SkillSangatKurang");
                    if (wm.Ada("SurvivalBuruk")) cf = CF(cf, wm.GetCF("SurvivalBuruk"));
                    if (wm.Ada("SkillKurang")) cf = CF(cf, wm.GetCF("SkillKurang"));
                    wm.SimpanCF("Difficulty:Easy", Math.Round(cf * 0.88, 2));
                    wm.SimpanAlasan("Difficulty:Easy",
                        "Sering terkena serangan boss — skill dodge dan defense kurang");
                }
            ));

            R.Add(new Rule(
                "EASY: hampir kalah, HP kritis",
                wm => wm.Ada("HampirKalah"),
                wm =>
                {
                    double cf = wm.GetCF("HampirKalah");
                    if (wm.Ada("SurvivalCukup")) cf = CF(cf, wm.GetCF("SurvivalCukup"));
                    if (wm.Ada("ResourceBuruk")) cf = CF(cf, wm.GetCF("ResourceBuruk"));
                    wm.SimpanCF("Difficulty:Easy", Math.Round(cf * 0.85, 2));
                    wm.SimpanAlasan("Difficulty:Easy",
                        "HP hampir habis dan boros potion — survival sangat terancam");
                }
            ));

            R.Add(new Rule(
                "EASY: pengalaman kurang meski tidak mati",
                wm => wm.Ada("PengalamanKurang") && wm.Ada("ResourceBuruk"),
                wm =>
                {
                    double cf = CF(wm.GetCF("PengalamanKurang"), wm.GetCF("ResourceBuruk"));
                    if (wm.Ada("SurvivalBagus")) cf = CF(cf, wm.GetCF("SurvivalBagus"));
                    if (wm.Ada("SkillSedang")) cf = CF(cf, wm.GetCF("SkillSedang"));
                    wm.SimpanCF("Difficulty:Easy", Math.Round(cf * 0.82, 2));
                    wm.SimpanAlasan("Difficulty:Easy",
                        "Pengalaman dan resource buruk meski berhasil menyelesaikan dungeon");
                }
            ));

            return R;
        }
    }

    class InferenceEngine
    {
        private List<Rule> _rules;

        public InferenceEngine(List<Rule> rules)
        {
            _rules = rules;
        }

        public void Jalankan(WorkingMemory wm)
        {
            string soal = "Q1";
            int nomor = 1;

            while (soal != null)
            {
                bool? jawab = TanyaUser(nomor, KnowledgeBase.Pertanyaan[soal]);

                if (jawab == null)
                {
                    wm.Log.Add("[" + nomor + "/4] " + soal + " = Tidak Tahu → lanjut ke hasil");
                    break;
                }

                wm.SimpanJawaban(soal, jawab.Value);

                wm.Log.Add("[" + nomor + "/4] " + soal +
                           " = " + (jawab.Value ? "Ya" : "Tidak"));

                JalankanGrup(wm, isGrup1: true);

                string keyPohon = soal + (jawab.Value ? "|Y" : "|N");

                string soalBerikutnya;
                KnowledgeBase.PohonSoal.TryGetValue(keyPohon, out soalBerikutnya);
                soal = soalBerikutnya;
                nomor++;
            }

            wm.Log.Add("--- Menentukan difficulty ---");
            JalankanGrup(wm, isGrup1: false);

            InferFallback(wm);
        }

        private void InferFallback(WorkingMemory wm)
        {
            bool adaDifficulty = wm.SemuaCF().Keys.Any(k => k.StartsWith("Difficulty:"));

            if (adaDifficulty) return;

            wm.Log.Add("  [Fallback] Data parsial — estimasi dari indikator awal...");

            if (wm.Ada("SurvivalBagus") && wm.Ada("SkillTinggi"))
            {
                double cf = KnowledgeBase.CF(wm.GetCF("SurvivalBagus"), wm.GetCF("SkillTinggi"));
                wm.SimpanCF("Difficulty:Hard", Math.Round(cf * 0.78, 2));
                wm.SimpanAlasan("Difficulty:Hard",
                    "Clear tanpa mati dan cepat (estimasi dari data yang belum lengkap)");
                wm.Log.Add("  Fallback: HARD — SurvivalBagus + SkillTinggi");
            }
            else if (wm.Ada("SurvivalBagus") && wm.Ada("SkillSedang"))
            {
                double cf = KnowledgeBase.CF(wm.GetCF("SurvivalBagus"), wm.GetCF("SkillSedang"));
                wm.SimpanCF("Difficulty:Normal", Math.Round(cf * 0.65, 2));
                wm.SimpanAlasan("Difficulty:Normal",
                    "Clear tanpa mati namun lambat (estimasi dari data yang belum lengkap)");
                wm.Log.Add("  Fallback: NORMAL — SurvivalBagus + SkillSedang");
            }
            else if (wm.Ada("SurvivalBuruk"))
            {
                double cf = wm.GetCF("SurvivalBuruk");
                wm.SimpanCF("Difficulty:Easy", Math.Round(cf * 0.80, 2));
                wm.SimpanAlasan("Difficulty:Easy",
                    "Player mati lebih dari 3 kali (estimasi dari data yang belum lengkap)");
                wm.Log.Add("  Fallback: EASY — SurvivalBuruk");
            }
            else if (wm.Ada("SurvivalCukup"))
            {
                double cf = wm.GetCF("SurvivalCukup");
                wm.SimpanCF("Difficulty:Normal", Math.Round(cf * 0.60, 2));
                wm.SimpanAlasan("Difficulty:Normal",
                    "Player mati 1-3 kali (estimasi dari data yang belum lengkap)");
                wm.Log.Add("  Fallback: NORMAL — SurvivalCukup");
            }
            else if (wm.Ada("SurvivalBagus"))
            {
                double cf = wm.GetCF("SurvivalBagus");
                wm.SimpanCF("Difficulty:Normal", Math.Round(cf * 0.55, 2));
                wm.SimpanAlasan("Difficulty:Normal",
                    "Hanya diketahui player clear tanpa mati (data sangat terbatas)");
                wm.Log.Add("  Fallback: NORMAL — hanya SurvivalBagus");
            }
            else if (wm.Tidak("Q1"))
            {
                wm.SimpanCF("Difficulty:Easy", 0.55);
                wm.SimpanAlasan("Difficulty:Easy",
                    "Player mati setidaknya sekali — data belum lengkap, perkiraan awal Easy");
                wm.Log.Add("  Fallback: EASY — Q1=Tidak, Q2B tidak terjawab");
            }
        }

        private void JalankanGrup(WorkingMemory wm, bool isGrup1)
        {
            bool adaYangJalan;
            do
            {
                adaYangJalan = false;

                foreach (Rule r in _rules)
                {
                    if (r.SudahJalan) continue;

                    bool iniGrup2 = r.Nama.Contains("LEGENDARY")
                                 || r.Nama.Contains("HARD")
                                 || r.Nama.Contains("NORMAL")
                                 || r.Nama.Contains("EASY");

                    if (isGrup1 && iniGrup2) continue;
                    if (!isGrup1 && !iniGrup2) continue;

                    if (r.KondisiTerpenuhi(wm))
                    {
                        r.Jalankan(wm);
                        r.SudahJalan = true;
                        adaYangJalan = true;
                        wm.Log.Add("  Rule aktif: " + r.Nama);
                    }
                }
            }
            while (adaYangJalan);
        }

        private bool? TanyaUser(int nomor, string pertanyaan)
        {
            Console.WriteLine();
            Console.WriteLine("  [" + nomor + "/4] " + pertanyaan);
            Console.Write("        [y = Ya  /  n = Tidak  /  Enter = tidak tahu]: ");

            string input = (Console.ReadLine() ?? "").Trim().ToLower();

            if (input == "y" || input == "ya") return true;
            if (input == "n" || input == "tidak") return false;
            return null;
        }
    }

    class Program
    {
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("================================================");
            Console.WriteLine("  SISTEM PAKAR DDA");
            Console.WriteLine("  Dynamic Difficulty Adjustment — Game Boss");
            Console.WriteLine("================================================");
            Console.WriteLine("  y = Ya  |  n = Tidak  |  Enter = tidak tahu");
            Console.WriteLine("------------------------------------------------");

            var wm = new WorkingMemory();

            var engine = new InferenceEngine(KnowledgeBase.BuatSemuaRule());

            engine.Jalankan(wm);
            TampilkanHasil(wm);
        }

        static void TampilkanHasil(WorkingMemory wm)
        {
            string diff = null;
            double cfMax = 0.0;

            foreach (var f in wm.SemuaCF())
            {
                if (!f.Key.StartsWith("Difficulty:")) continue;

                if (f.Value > cfMax)
                {
                    cfMax = f.Value;
                    diff = f.Key.Replace("Difficulty:", "");
                }
            }

            Console.WriteLine();
            Console.WriteLine("================================================");
            Console.WriteLine("  HASIL DIAGNOSIS DIFFICULTY");
            Console.WriteLine("================================================");

            if (diff == null)
            {
                Console.WriteLine();
                Console.WriteLine("  Belum cukup informasi untuk menentukan difficulty.");
                Console.WriteLine("  Coba jawab minimal 1 pertanyaan.");
            }
            else
            {
                string yakin = cfMax >= 0.85 ? "Sangat Yakin"
                             : cfMax >= 0.65 ? "Cukup Yakin"
                             : "Perkiraan";

                Console.WriteLine();
                Console.WriteLine("  Difficulty : " + diff.ToUpper());
                Console.WriteLine("  Boss       : " + NamaBoss(diff));
                Console.WriteLine("  Alasan Boss: " + AlasanBoss(diff));
                Console.WriteLine("  Keyakinan  : " + yakin +
                                  " (" + (cfMax * 100).ToString("F0") + "%)");
                Console.WriteLine("  Keterangan : " + Keterangan(diff));
                Console.WriteLine("  Alasan     : " + wm.GetAlasan("Difficulty:" + diff));
            }

            Console.WriteLine();
            Console.WriteLine("  Indikator yang Terdeteksi:");
            foreach (var f in wm.SemuaCF())
            {
                if (f.Key.StartsWith("Difficulty:")) continue;
                Console.WriteLine("    " + f.Key.PadRight(22) +
                                  (f.Value * 100).ToString("F0") + "%");
            }

            Console.WriteLine();
            Console.WriteLine("================================================");
            Console.WriteLine("  JEJAK PENALARAN (Rule yang Aktif)");
            Console.WriteLine("================================================");
            foreach (string baris in wm.Log)
                Console.WriteLine("  " + baris);

            Console.WriteLine();
            Console.WriteLine("  Tekan Enter untuk keluar...");
            Console.ReadLine();
        }

        static string NamaBoss(string d)
        {
            if (d == "Legendary") return "Naga Kuno (Boss Tertinggi)";
            if (d == "Hard") return "Naga";
            if (d == "Normal") return "Golem";
            if (d == "Easy") return "Goblin";
            return d;
        }

        static string AlasanBoss(string d)
        {
            if (d == "Legendary") return "Boss tertinggi karena performa sempurna di semua kategori";
            if (d == "Hard") return "Boss kuat karena player sudah berpengalaman dan efisien";
            if (d == "Normal") return "Boss standar karena performa seimbang, masih ada ruang berkembang";
            if (d == "Easy") return "Boss ringan agar player bisa belajar tanpa frustrasi";
            return "-";
        }

        static string Keterangan(string d)
        {
            if (d == "Legendary") return "Player sangat mahir! Tantangan tertinggi menanti.";
            if (d == "Hard") return "Player berpengalaman. Boss kuat untuk tantangan optimal.";
            if (d == "Normal") return "Performa seimbang. Boss standar yang sesuai.";
            if (d == "Easy") return "Player masih berkembang. Boss ringan untuk membangun skill.";
            return "-";
        }
    }
}