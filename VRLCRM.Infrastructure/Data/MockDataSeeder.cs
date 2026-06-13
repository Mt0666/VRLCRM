using Microsoft.EntityFrameworkCore;
using VRLCRM.Domain.Entities;
using VRLCRM.Domain.Enums;

namespace VRLCRM.Infrastructure.Data;

public static class MockDataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        if (await db.Customers.AnyAsync())
            return;

        var rng = new Random(42);
        var now = DateTime.UtcNow;

        // ── 1. KATEGORİLER ────────────────────────────────────────────────────
        var categoryNames = new[]
        {
            "Elektronik", "Bilgisayar & Laptop", "Telefon & Aksesuar", "TV & Ses Sistemleri",
            "Beyaz Eşya", "Küçük Ev Aletleri", "Aydınlatma", "Klima & Isıtma",
            "Mobilya", "Yatak Odası", "Oturma Odası", "Ofis Mobilyaları", "Mutfak",
            "Erkek Giyim", "Kadın Giyim", "Çocuk Giyim", "Ayakkabı", "Çanta & Aksesuar",
            "Tahıl & Bakliyat", "İçecek", "Süt & Süt Ürünleri", "Konserve & Hazır Gıda",
            "Temizlik Ürünleri", "Kişisel Bakım", "Parfüm & Kozmetik",
            "Kırtasiye", "Ofis Malzemeleri", "Kağıt & Ambalaj",
            "Boya & Badana", "Hırdavat & Alet", "İnşaat Malzemeleri", "Boru & Tesisat",
            "Spor Malzemeleri", "Fitness & Gym", "Outdoor & Kamp",
            "Otomotiv Aksesuar", "Lastik & Jant", "Yedek Parça",
            "Bahçe & Peyzaj", "Tarım & Hayvancılık"
        };

        var categories = categoryNames.Select((n, i) => new Category
        {
            Name = n,
            IsActive = true,
            CreatedAt = now.AddDays(-rng.Next(180, 400)),
            CreatedBy = "sistem"
        }).ToList();

        db.Categories.AddRange(categories);
        await db.SaveChangesAsync();

        // ── 2. TEDARİKÇİLER ───────────────────────────────────────────────────
        var supplierData = new[]
        {
            ("Anadolu Elektronik A.Ş.", "Mehmet Yılmaz", "0212 555 10 20", "İstanbul", "Bağcılar"),
            ("Güneş Tekstil Ltd. Şti.", "Ayşe Kara", "0232 444 30 40", "İzmir", "Konak"),
            ("Marmara Gıda San. A.Ş.", "Hüseyin Demir", "0224 333 50 60", "Bursa", "Nilüfer"),
            ("Ege Mobilya Fab. A.Ş.", "Fatma Şahin", "0232 222 70 80", "İzmir", "Bornova"),
            ("Ankara İnşaat Malz. Ltd.", "Ali Çelik", "0312 111 90 00", "Ankara", "Etimesgut"),
            ("Karadeniz Tarım Ürün. A.Ş.", "Zeynep Aydın", "0462 555 11 22", "Trabzon", "Ortahisar"),
            ("Boğaz Temizlik Ürün. Ltd.", "İbrahim Kurt", "0216 444 33 44", "İstanbul", "Pendik"),
            ("Atatürk Kırtasiye A.Ş.", "Merve Doğan", "0312 333 55 66", "Ankara", "Çankaya"),
            ("Yıldız Otomotiv Ltd. Şti.", "Mustafa Arslan", "0222 222 77 88", "Eskişehir", "Tepebaşı"),
            ("Akdeniz Spor Malz. A.Ş.", "Elif Yıldız", "0242 111 99 00", "Antalya", "Kepez"),
            ("Pamukkale Tekstil A.Ş.", "Osman Öztürk", "0258 555 12 23", "Denizli", "Merkezefendi"),
            ("Fırat Gıda San. Ltd.", "Hatice Koç", "0424 444 34 45", "Elazığ", "Merkez"),
            ("Toros Boya Ltd. Şti.", "Recep Polat", "0322 333 56 67", "Adana", "Seyhan"),
            ("Kafkas Elektronik A.Ş.", "Sevgi Kaya", "0442 222 78 89", "Erzurum", "Yakutiye"),
            ("Ege Beyaz Eşya Ltd.", "Bülent Yıldırım", "0232 111 90 01", "İzmir", "Karşıyaka"),
            ("Marmara Hırdavat A.Ş.", "Gülşen Aktaş", "0212 555 23 34", "İstanbul", "Sultangazi"),
            ("Anadolu Kozmetik Ltd.", "Serkan Erdoğan", "0332 444 45 56", "Konya", "Selçuklu"),
            ("Güney Mobilya San. A.Ş.", "Nurdan Çetin", "0342 333 67 78", "Gaziantep", "Şehitkamil"),
            ("Batı İlaç & Medikal Ltd.", "Kemal Tunç", "0274 222 89 90", "Kütahya", "Merkez"),
            ("Doğu Tarım Makine A.Ş.", "Perihan Güler", "0412 111 01 12", "Diyarbakır", "Kayapınar"),
        };

        var suppliers = supplierData.Select((s, i) => new Supplier
        {
            CompanyName = s.Item1,
            ContactName = s.Item2,
            PhoneNumber = s.Item3,
            TaxNumber = $"TR{rng.Next(1000000, 9999999)}",
            City = s.Item4,
            District = s.Item5,
            Balance = 0,
            CreditLimit = rng.Next(5, 20) * 10000m,
            IsActive = true,
            CreatedAt = now.AddDays(-rng.Next(200, 500)),
            CreatedBy = "sistem"
        }).ToList();

        db.Suppliers.AddRange(suppliers);
        await db.SaveChangesAsync();

        // ── 3. ÜRÜNLER (500) ──────────────────────────────────────────────────
        var productTemplates = new Dictionary<string, (string[] Names, decimal MinPrice, decimal MaxPrice, int VatRate)>
        {
            ["Elektronik"]              = (["Güç Kaynağı 650W", "UPS 1000VA", "HDMI Kablo 2m", "USB Hub 4 Port", "Şarj Aleti 65W", "Ethernet Switch 8 Port", "Wireless Adaptör", "Bluetooth Hoparlör", "Akıllı Priz", "Dijital Multimetre"], 150, 2500, 20),
            ["Bilgisayar & Laptop"]     = (["Laptop 15.6\" i5", "Laptop 14\" i7", "Masaüstü PC i5", "Oyuncu PC RTX3060", "Mini PC Intel", "Monitör 24\" FHD", "Monitör 27\" 4K", "Klavye Mekanik", "Mouse Kablosuz", "Webcam 1080p", "Laptop Çantası", "Mouse Pad XL"], 800, 25000, 20),
            ["Telefon & Aksesuar"]      = (["Akıllı Telefon 128GB", "Akıllı Telefon 256GB", "Tablet 10.4\"", "Telefon Kılıfı", "Ekran Koruyucu", "Kablosuz Kulaklık", "Powerbank 20000mAh", "Hızlı Şarj Kablosu", "Telefon Standı", "Selfie Stick"], 50, 20000, 20),
            ["TV & Ses Sistemleri"]     = (["LED TV 43\"", "LED TV 55\"", "OLED TV 65\"", "Soundbar 2.1", "Ev Sinema Sistemi", "Projeksiyon Cihazı", "DVD Oynatıcı", "Uydu Alıcısı", "Kulaklık Over-Ear", "Akıllı TV Box"], 500, 35000, 20),
            ["Beyaz Eşya"]             = (["Çamaşır Makinesi 9kg", "Bulaşık Makinesi", "Buzdolabı No-Frost", "Derin Dondurucu", "Fırın Ankastre", "Ocak 4 Gözlü", "Davlumbaz", "Çamaşır Kurutma", "Mini Buzdolabı", "Buz Makinesi"], 3000, 25000, 20),
            ["Küçük Ev Aletleri"]      = (["Kahve Makinesi", "Blender Set", "Ekmek Kızartma", "Su Isıtıcı", "Elektrikli Süpürge", "Robot Süpürge", "Ütü Buharlı", "Saç Kurutma", "Mikser", "Tost Makinesi", "Çok Pişirici", "Hava Fritözü"], 150, 3500, 20),
            ["Aydınlatma"]             = (["LED Ampul E27 9W", "LED Ampul E14 6W", "Sarkıt Avize", "Spot Armatür", "LED Panel 60x60", "Şerit LED 5m", "Masa Lambası", "Aplik Duvar", "Bahçe Lambası Solar", "Projeksiyon LED"], 20, 1500, 20),
            ["Klima & Isıtma"]         = (["Klima 9000 BTU", "Klima 12000 BTU", "Klima 18000 BTU", "Kombi Doğalgaz", "Elektrikli Isıtıcı", "Radyatör Panel", "Klima Bakım Seti", "Termostat Dijital", "Fan Tavan", "Vantilatör"], 800, 20000, 20),
            ["Mobilya"]                = (["Çalışma Masası", "Kitaplık 5 Raflı", "TV Ünitesi", "Konsol Masası", "Bahçe Masası Set", "Müzik Sehpası", "Vitrin Camlı", "Sepet Bambu", "Puf Osmanlı", "Kapı Önü Paspas"], 200, 5000, 20),
            ["Yatak Odası"]            = (["Çift Kişilik Karyola", "Tek Kişilik Karyola", "Komodin 2 Çekmeceli", "Şifonyer 5 Çekmece", "Ayna Geniş", "Baza 160x200", "Yatak Yaylı 160x200", "Yastık Visco", "Nevresim Takımı", "Çarşaf Seti"], 300, 8000, 20),
            ["Oturma Odası"]           = (["Köşe Koltuk Takımı", "3+1+1 Koltuk", "Sehpa Merkez", "TV Sehpası", "Halı 160x230", "Kanepe 2'li", "Puf Büyük", "Duvar Saati", "Çiçeklik Ahşap", "Abajur"], 500, 15000, 20),
            ["Ofis Mobilyaları"]       = (["Ofis Koltuğu Ergonomik", "Çalışma Koltuğu", "Toplantı Masası", "Dosya Dolabı Metal", "Bilgisayar Masası", "Bölme Panel", "Misafir Koltuğu", "Kitaplık Ofis", "Ayaklı Askı", "Kasa Çelik"], 400, 12000, 20),
            ["Mutfak"]                 = (["Tencere Seti 7 Parça", "Tava Döküm", "Bıçak Seti", "Kesme Tahtası", "Kase Seti", "Vakumlu Saklama", "Mutfak Tartısı", "Çöp Kovası Pedallı", "Süzgeç Seti", "Düdüklü Tencere"], 100, 2000, 20),
            ["Erkek Giyim"]            = (["Polo Yaka T-Shirt", "Slim Fit Pantolon", "Klasik Gömlek", "Triko Kazak", "Mont Kış", "Eşofman Takımı", "Spor Şort", "Denim Pantolon", "Kaban Oversize", "İç Çamaşırı Set"], 100, 1500, 10),
            ["Kadın Giyim"]            = (["Bluz Şifon", "Elbise Midi", "Pantolon Yüksek Bel", "Ceket Blazer", "Trençkot", "Kazak Oversize", "Etek Midi", "Spor Tayt", "Pijama Takımı", "Hırka Örgü"], 100, 2000, 10),
            ["Çocuk Giyim"]            = (["Bebek Body Set", "Çocuk Takım", "Okul Önlüğü", "Polar Hırka", "Yağmurluk Çocuk", "Çocuk Pijama", "Bebek Tulumu", "İlkbahar Mont", "Çocuk Şort", "Çorap Seti 5'li"], 50, 500, 10),
            ["Ayakkabı"]               = (["Erkek Deri Ayakkabı", "Bayan Topuklu", "Spor Ayakkabı", "Bot Kışlık", "Sandalet Yazlık", "Terlik Ev", "Converse Benzeri", "Çocuk Spor Ayk.", "Loafer Erkek", "Babet Bayan"], 150, 2500, 10),
            ["Çanta & Aksesuar"]       = (["Sırt Çantası 30L", "El Çantası Deri", "Omuz Çantası", "Bel Çantası", "Seyahat Valiz", "Laptop Çantası 15\"", "Cüzdan Erkek", "Kemer Deri", "Şapka Kışlık", "Kravat Set"], 100, 3000, 10),
            ["Tahıl & Bakliyat"]       = (["Un Buğday 5kg", "Pirinç Baldo 5kg", "Nohut 1kg", "Kırmızı Mercimek 1kg", "Fasulye Kuru 1kg", "Bulgur İnce 2kg", "Mısır Unu 1kg", "Arpa Unu 1kg", "Yulaf Ezmesi 1kg", "Makarna 500gr"], 20, 200, 1),
            ["İçecek"]                 = (["Su 0.5L Koli 24'lü", "Su 1.5L Koli 12'li", "Maden Suyu 6'lı", "Ayran 200ml 12'li", "Meyve Suyu 1L", "Kola 2.5L", "Çay 1kg", "Kahve Granül 200gr", "Bitki Çayı Set", "Enerji İçeceği 250ml 24'lü"], 30, 500, 8),
            ["Süt & Süt Ürünleri"]     = (["Süt 1L UHT", "Yoğurt 1kg", "Beyaz Peynir 500gr", "Kaşar Peynir 500gr", "Tereyağı 250gr", "Krema 200ml", "Ayran 1L", "Labne 300gr", "Kefir 500ml", "Lor Peyniri 250gr"], 15, 250, 8),
            ["Konserve & Hazır Gıda"]  = (["Domates Konserve 400gr", "Mısır Konserve 212ml", "Ton Balığı 160gr", "Zeytin Yeşil 400gr", "Ketçap 450gr", "Mayonez 500gr", "Salça Domates 700gr", "Hazır Çorba Paket", "Makarna Sos 300gr", "Reçel 380gr"], 10, 150, 8),
            ["Temizlik Ürünleri"]      = (["Çamaşır Deterjanı 5kg", "Bulaşık Deterjanı 750ml", "Yüzey Temizleyici 750ml", "Klor Javel 1L", "Yer Temizleyici 1L", "Cam Temizleyici 500ml", "Çöp Poşeti 30L 20'li", "Tuvalet Kağıdı 32'li", "Kağıt Havlu 12'li", "Deterjan Kapsül 30'lu"], 20, 300, 20),
            ["Kişisel Bakım"]          = (["Şampuan 400ml", "Saç Kremi 350ml", "Duş Jeli 400ml", "Deodorant 150ml", "Diş Macunu 125ml", "Diş Fırçası Set", "Tıraş Köpüğü 200ml", "El Kremi 75ml", "Güneş Kremi SPF50", "Pamuk 100gr"], 10, 200, 20),
            ["Parfüm & Kozmetik"]      = (["Parfüm EDT 100ml E", "Parfüm EDP 50ml B", "Ruj Mat", "Fondöten SPF15", "Maskara Siyah", "Göz Kalemi", "Allık", "Oje Set 12 Renk", "Makyaj Fırçası Set", "BB Krem"], 50, 1500, 20),
            ["Kırtasiye"]              = (["Tükenmez Kalem 50'li", "Kurşun Kalem 2B 12'li", "Silgi Beyaz", "Cetvel 30cm", "Makas Ofis", "Zımba Makinesi", "Delgeç 30 Yaprak", "Post-it 3x3 100'lü", "Yapışkanlı Not", "Kâğıt Klips 50'li"], 5, 200, 20),
            ["Ofis Malzemeleri"]       = (["Klasör Geniş Sırtlı", "Dosya Askılı 25'li", "Zarf C4 25'li", "Etiket 105x37mm", "Bant Şeffaf 19mm", "CD-DVD Çanta 24'lü", "Kalem Kutusu", "Masa Üstü Düzenleyici", "Beyaz Tahta 90x120", "Projeksiyon Perdesi"], 10, 800, 20),
            ["Kağıt & Ambalaj"]        = (["A4 Fotokopi Kağıdı 500'lü", "A3 Kağıt 250'li", "Karton Kutu 40x30x30", "Balonlu Naylon 50m", "Streç Film 20cm", "Kraft Kağıt 70gr", "Koli Bandı 6'lı Set", "Köpük Bant 5m", "Etiket Sticket A4", "Dosyalık Karton"], 20, 400, 20),
            ["Boya & Badana"]          = (["İç Cephe Boyası 15kg", "Dış Cephe Boyası 15kg", "Astar 10kg", "Alçı Yapı 25kg", "Macun Dolgu 10kg", "Rulo Yedek 4'lü", "Fırça Set 4 Adet", "Mastar Karıştırıcı", "Bant Maskeleme", "Nitro Sprey Boya 400ml"], 50, 1500, 20),
            ["Hırdavat & Alet"]        = (["Matkap Akülü 18V", "Testere Daire", "Taşlama Makinesi 125mm", "Çekiç 500gr", "Tornavida Set 12'li", "Pense Kombine", "Somun Anahtarı Set", "Seviye Aleti 60cm", "Mezura 5m", "Tel Kafes Raf Set"], 50, 3000, 20),
            ["İnşaat Malzemeleri"]     = (["Çimento 25kg", "Kum İnce Torba 25kg", "Tuğla Bims 380", "Demir Hasır 50x50", "Strafor Levha 5cm", "Alçıpan Levha", "Profil C 75mm", "Dübel Set 100'lü", "Yapı Tutkalı 25kg", "Silikon Mastik 310ml"], 30, 500, 20),
            ["Boru & Tesisat"]         = (["Plastik Boru PPR 20mm", "PVC Boru 100mm", "Vana Küresel 3/4\"", "Musluk Tek Kollu", "Rezervuar Komple", "Sifon Küvet", "Klozet Asma", "Sıhhi Bant Teflon", "Kaynak Makinesi PP", "Prese Mengene"], 20, 2000, 20),
            ["Spor Malzemeleri"]       = (["Futbol Topu No.5", "Voleybol Topu", "Basketbol Sayaç", "Tenis Raketi", "Masa Tenisi Set", "Badminton Set", "İp Atlama", "Kafa Bandı 3'lü", "Sporcu Çorabı 5'li", "Su Matarası 750ml"], 30, 800, 10),
            ["Fitness & Gym"]          = (["Dumbbell 2x5kg", "Dumbbell 2x10kg", "Yoga Matı 6mm", "Direnç Bandı Set", "Karın Aleti", "Sırt Egzersiz Aleti", "Halter Seti 20kg", "Kol Pronatör", "Sporcu Eldiveni", "Protein Shaker 750ml"], 50, 2500, 10),
            ["Outdoor & Kamp"]         = (["Çadır 2 Kişilik", "Uyku Tulumu -5°C", "Kamp Sandalyesi", "Kamp Sobası Gaz", "El Feneri LED", "Bıçak Çakı", "Termos 1L", "Kamp Seti Yemek", "Hamak Çift", "Sırt Çantası 55L"], 100, 3000, 20),
            ["Otomotiv Aksesuar"]      = (["Araba Paspası Set", "Araç İçi Kamera", "Park Sensörü", "Far Ampulü H7 2'li", "Silecek 65cm", "Araç Şarj 2 USB", "Lastik Şişirme 12V", "Araç Parfümü", "Reflektör Set", "Oto Klima Spreyi"], 30, 1500, 20),
            ["Lastik & Jant"]          = (["Lastik 185/65R15", "Lastik 205/55R16", "Lastik 225/45R17", "Jant Çelik 15\"", "Jant Alüminyum 16\"", "Lastik Zinciri", "Lastik Basınç Göstergesi", "Jant Kapağı 4'lü", "Vida Tekerlek M12", "Balans Ağırlığı 60gr"], 200, 3000, 20),
            ["Yedek Parça"]            = (["Yağ Filtresi", "Hava Filtresi", "Polen Filtresi", "Motor Yağı 5W-30 4L", "Fren Balatası Ön", "Fren Diski Çift", "Akü 60Ah", "Buji Seti 4'lü", "Termostat", "Alternator Fırçası"], 30, 2500, 20),
            ["Bahçe & Peyzaj"]         = (["Çim Biçme Makinesi", "Elektrikli Çit Makinesi", "Bahçe Hortumu 25m", "Sulama Başlığı Set", "Bahçe Eldiveni", "Budama Makası", "Kürek Bahçe", "Tırmık Çelik", "Saksı Set 5'li", "Toprak Çiçek 40L"], 30, 2000, 20),
            ["Tarım & Hayvancılık"]    = (["Gübre NPK 25kg", "Zirai İlaç 1L", "Damla Sulama Seti", "Mini Traktör Ekipmanı", "Hayvan Yemi 25kg", "Su Yalağı 50L", "Kafes Kümes Tel", "Hayvan Aşısı", "Sulama Pompası", "Toprak pH Test Kiti"], 50, 5000, 20),
        };

        var stockItems = new List<StockItem>();
        int stockCode = 1000;
        foreach (var cat in categories)
        {
            if (!productTemplates.TryGetValue(cat.Name, out var tmpl))
                continue;

            var (names, minP, maxP, vat) = tmpl;
            var count = Math.Max(1, 500 / categories.Count);
            var used = new HashSet<string>();

            for (int i = 0; i < count; i++)
            {
                var baseName = names[i % names.Length];
                var suffix = i >= names.Length ? $" {i / names.Length + 2}" : "";
                var name = baseName + suffix;
                var price = Math.Round(minP + (decimal)(rng.NextDouble() * (double)(maxP - minP)), 2);
                stockItems.Add(new StockItem
                {
                    StockCode = $"STK-{++stockCode:D5}",
                    CategoryId = cat.Id,
                    Name = name,
                    Price = price,
                    VatRate = vat,
                    StockQuantity = 0,
                    CriticalStockLevel = rng.Next(5, 21),
                    IsActive = true,
                    CreatedAt = now.AddDays(-rng.Next(100, 350)),
                    CreatedBy = "sistem"
                });
            }
        }
        // Toplam ~500 olsun, arta kalan eksikse doldur
        while (stockItems.Count < 500)
        {
            var cat = categories[rng.Next(categories.Count)];
            var price = Math.Round((decimal)(100 + rng.NextDouble() * 4900), 2);
            stockItems.Add(new StockItem
            {
                StockCode = $"STK-{++stockCode:D5}",
                CategoryId = cat.Id,
                Name = $"Genel Ürün {stockCode}",
                Price = price,
                VatRate = 20,
                StockQuantity = 0,
                CriticalStockLevel = rng.Next(5, 21),
                IsActive = true,
                CreatedAt = now.AddDays(-rng.Next(100, 350)),
                CreatedBy = "sistem"
            });
        }

        db.StockItems.AddRange(stockItems);
        await db.SaveChangesAsync();

        // ── 4. MÜŞTERİLER (100) ──────────────────────────────────────────────
        var firstNames = new[] { "Ahmet", "Mehmet", "Ali", "Hüseyin", "İbrahim", "Mustafa", "Ömer", "Fatih", "Emre", "Burak",
                                  "Ayşe", "Fatma", "Zeynep", "Emine", "Hatice", "Merve", "Elif", "Selin", "Gül", "Nur",
                                  "Recep", "Serkan", "Oğuz", "Kemal", "Caner", "Tolga", "Murat", "Bülent", "Ercan", "Tayfun",
                                  "Yasemin", "Özlem", "Derya", "Sevgi", "Dilek", "Pınar", "Aslı", "Burcu", "Tuğba", "Ece",
                                  "Hasan", "Kazım", "Levent", "Nuri", "Orhan", "Selim", "Tarık", "Umut", "Volkan", "Yusuf" };
        var lastNames = new[] { "Yılmaz", "Kaya", "Demir", "Şahin", "Çelik", "Yıldız", "Yıldırım", "Öztürk", "Arslan", "Doğan",
                                 "Kılıç", "Aslan", "Çetin", "Koç", "Kurt", "Aydın", "Özdemir", "Erdoğan", "Polat", "Güneş",
                                 "Bulut", "Aktaş", "Kaplan", "Korkmaz", "Acar", "Şimşek", "Güler", "Çakır", "Tunç", "Yalçın",
                                 "Duman", "Erdal", "Fidan", "Güven", "Işık", "Karataş", "Liman", "Mert", "Nalbant", "Parlak" };
        var cities = new[] { "İstanbul", "Ankara", "İzmir", "Bursa", "Antalya", "Adana", "Konya", "Gaziantep", "Mersin", "Eskişehir",
                              "Kayseri", "Trabzon", "Samsun", "Malatya", "Diyarbakır", "Erzurum", "Sakarya", "Denizli", "Manisa", "Kocaeli" };
        var districts = new[] { "Merkez", "Bağcılar", "Kadıköy", "Üsküdar", "Beşiktaş", "Şişli", "Nilüfer", "Konak", "Kepez", "Selçuklu",
                                  "Çankaya", "Etimesgut", "Bornova", "Karşıyaka", "Seyhan", "Şehitkamil", "Kayapınar", "Ortahisar", "Pamukkale", "Yunusemre" };
        var companies = new[] { null, "A.Ş.", "Ltd. Şti.", "Ticaret", "İnşaat", "Tekstil", "Gıda", "Elektronik", "Mobilya", "Otomotiv" };

        var customers = new List<Customer>();
        for (int i = 0; i < 100; i++)
        {
            var fn = firstNames[rng.Next(firstNames.Length)];
            var ln = lastNames[rng.Next(lastNames.Length)];
            var co = rng.Next(3) == 0 ? null : $"{ln} {companies[rng.Next(1, companies.Length)]}";
            var city = cities[rng.Next(cities.Length)];
            var district = districts[rng.Next(districts.Length)];
            customers.Add(new Customer
            {
                FirstName = fn,
                LastName = ln,
                CompanyName = co,
                PhoneNumber = $"05{rng.Next(30, 59)}{rng.Next(1000000, 9999999)}",
                Balance = 0,
                CreditLimit = rng.Next(4) == 0 ? null : rng.Next(5, 50) * 5000m,
                IsActive = true,
                CreatedAt = now.AddDays(-rng.Next(30, 400)),
                CreatedBy = "sistem",
                Address = new Address
                {
                    City = city,
                    District = district,
                    AddressLine = $"{streets[rng.Next(streets.Length)]} No:{rng.Next(1, 200)} Daire:{rng.Next(1, 20)}",
                    IsActive = true,
                    CreatedAt = now.AddDays(-rng.Next(30, 400)),
                    CreatedBy = "sistem"
                }
            });
        }
        db.Customers.AddRange(customers);
        await db.SaveChangesAsync();

        // ── 5. ALIM FAT. (100) → stok artar ──────────────────────────────────
        var movements = new List<StockMovement>();
        int invoiceSeq = 1, orderSeq = 1, paySeq = 1;

        for (int i = 0; i < 100; i++)
        {
            var supplier = suppliers[rng.Next(suppliers.Count)];
            var date = now.AddDays(-rng.Next(10, 360));
            var lineCount = rng.Next(2, 8);
            var lines = new List<InvoiceLine>();

            for (int l = 0; l < lineCount; l++)
            {
                var item = stockItems[rng.Next(stockItems.Count)];
                var qty = rng.Next(10, 200);
                var price = Math.Round(item.Price * (decimal)(0.5 + rng.NextDouble() * 0.3), 2);
                var vatRate = item.VatRate;
                var vatAmt = Math.Round(price * qty * vatRate / 100, 2);
                var lineTotal = price * qty + vatAmt;

                lines.Add(new InvoiceLine
                {
                    StockItemId = item.Id,
                    Quantity = qty,
                    UnitPrice = price,
                    VatRate = vatRate,
                    VatAmount = vatAmt,
                    LineTotal = lineTotal,
                    IsActive = true,
                    CreatedAt = date,
                    CreatedBy = "sistem"
                });

                item.StockQuantity += qty;
            }

            var subTotal = lines.Sum(l => l.UnitPrice * l.Quantity);
            var vatTotal = lines.Sum(l => l.VatAmount);
            var total = subTotal + vatTotal;

            var inv = new Invoice
            {
                InvoiceNumber = $"ALF-{date.Year}-{invoiceSeq++:D5}",
                InvoiceType = InvoiceType.Purchase,
                InvoiceDate = date,
                SupplierId = supplier.Id,
                SubTotal = subTotal,
                VatTotal = vatTotal,
                TotalAmount = total,
                Lines = lines,
                IsActive = true,
                CreatedAt = date,
                CreatedBy = "sistem"
            };
            db.Invoices.Add(inv);
            supplier.Balance += total;

            // stok hareketi — sonra Id bilineceği için kaydet
            movements.Add(null!); // placeholder, aşağıda dolduracağız
        }

        await db.SaveChangesAsync();
        db.StockItems.UpdateRange(stockItems);
        await db.SaveChangesAsync();

        // Alım faturalarına ait stok hareketlerini ekle
        var purchaseInvoices = await db.Invoices
            .Where(i => i.InvoiceType == InvoiceType.Purchase)
            .Include(i => i.Lines)
            .ToListAsync();

        foreach (var inv in purchaseInvoices)
        {
            foreach (var line in inv.Lines)
            {
                db.StockMovements.Add(new StockMovement
                {
                    StockItemId = line.StockItemId,
                    MovementType = StockMovementType.In,
                    Quantity = line.Quantity,
                    ReferenceType = StockMovementReferenceType.Invoice,
                    ReferenceId = inv.Id,
                    MovementDate = inv.InvoiceDate,
                    Notes = $"Alım faturası: {inv.InvoiceNumber}",
                    IsActive = true,
                    CreatedAt = inv.InvoiceDate,
                    CreatedBy = "sistem"
                });
            }
        }

        // ── 6. SİPARİŞLER (200) ──────────────────────────────────────────────
        var orders = new List<Order>();
        for (int i = 0; i < 200; i++)
        {
            var customer = customers[rng.Next(customers.Count)];
            var date = now.AddDays(-rng.Next(1, 300));
            var status = rng.Next(10) < 8 ? OrderStatus.Approved : (rng.Next(2) == 0 ? OrderStatus.Pending : OrderStatus.Cancelled);
            var lineCount = rng.Next(1, 6);
            var lines = new List<OrderLine>();

            for (int l = 0; l < lineCount; l++)
            {
                var item = stockItems[rng.Next(stockItems.Count)];
                var qty = rng.Next(1, 20);
                var price = item.Price;
                var vatRate = item.VatRate;
                var vatAmt = Math.Round(price * qty * vatRate / 100, 2);
                var lineTotal = price * qty + vatAmt;

                lines.Add(new OrderLine
                {
                    StockItemId = item.Id,
                    Quantity = qty,
                    UnitPrice = price,
                    VatRate = vatRate,
                    VatAmount = vatAmt,
                    LineTotal = lineTotal,
                    IsActive = true,
                    CreatedAt = date,
                    CreatedBy = "sistem"
                });

                if (status == OrderStatus.Approved)
                    item.StockQuantity = Math.Max(0, item.StockQuantity - qty);
            }

            var subTotal = lines.Sum(l => l.UnitPrice * l.Quantity);
            var vatTotal = lines.Sum(l => l.VatAmount);
            var total = subTotal + vatTotal;

            var order = new Order
            {
                OrderNumber = $"SIP-{date.Year}-{orderSeq++:D5}",
                CustomerId = customer.Id,
                OrderDate = date,
                Status = status,
                SubTotal = subTotal,
                VatTotal = vatTotal,
                TotalAmount = total,
                Lines = lines,
                IsActive = true,
                CreatedAt = date,
                CreatedBy = "sistem"
            };
            orders.Add(order);
            db.Orders.Add(order);
        }

        await db.SaveChangesAsync();
        db.StockItems.UpdateRange(stockItems);
        await db.SaveChangesAsync();

        // Onaylanan siparişler için stok hareketi
        var savedOrders = await db.Orders.Include(o => o.Lines).ToListAsync();
        foreach (var order in savedOrders.Where(o => o.Status == OrderStatus.Approved))
        {
            foreach (var line in order.Lines)
            {
                db.StockMovements.Add(new StockMovement
                {
                    StockItemId = line.StockItemId,
                    MovementType = StockMovementType.Out,
                    Quantity = line.Quantity,
                    ReferenceType = StockMovementReferenceType.Order,
                    ReferenceId = order.Id,
                    MovementDate = order.OrderDate,
                    Notes = $"Sipariş: {order.OrderNumber}",
                    IsActive = true,
                    CreatedAt = order.OrderDate,
                    CreatedBy = "sistem"
                });
            }
        }

        // ── 7. SATIŞ FATURALARI (150, onaylı siparişlerden) ─────────────────
        var approvedOrders = savedOrders.Where(o => o.Status == OrderStatus.Approved).Take(150).ToList();
        var salesInvoices = new List<Invoice>();
        foreach (var order in approvedOrders)
        {
            var customer = customers.First(c => c.Id == order.CustomerId);
            var invDate = order.OrderDate.AddDays(rng.Next(0, 3));

            var lines = order.Lines.Select(ol => new InvoiceLine
            {
                StockItemId = ol.StockItemId,
                Quantity = ol.Quantity,
                UnitPrice = ol.UnitPrice,
                VatRate = ol.VatRate,
                VatAmount = ol.VatAmount,
                LineTotal = ol.LineTotal,
                IsActive = true,
                CreatedAt = invDate,
                CreatedBy = "sistem"
            }).ToList();

            var inv = new Invoice
            {
                InvoiceNumber = $"FTR-{invDate.Year}-{invoiceSeq++:D5}",
                InvoiceType = InvoiceType.Sales,
                InvoiceDate = invDate,
                CustomerId = customer.Id,
                SubTotal = order.SubTotal,
                VatTotal = order.VatTotal,
                TotalAmount = order.TotalAmount,
                Notes = $"Sipariş: {order.OrderNumber}",
                Lines = lines,
                IsActive = true,
                CreatedAt = invDate,
                CreatedBy = "sistem"
            };
            salesInvoices.Add(inv);
            db.Invoices.Add(inv);
            customer.Balance += order.TotalAmount;
        }
        await db.SaveChangesAsync();

        // Satış faturalarını siparişe bağla
        var savedSalesInvoices = await db.Invoices
            .Where(i => i.InvoiceType == InvoiceType.Sales)
            .ToListAsync();
        for (int i = 0; i < approvedOrders.Count && i < savedSalesInvoices.Count; i++)
        {
            approvedOrders[i].SalesInvoiceId = savedSalesInvoices[i].Id;
        }
        await db.SaveChangesAsync();

        // ── 8. TAHSİLATLAR (müşterilerden) ──────────────────────────────────
        var paymentMethods = Enum.GetValues<PaymentMethod>();
        foreach (var customer in customers.Where(c => c.Balance > 0))
        {
            var payCount = rng.Next(1, 4);
            for (int p = 0; p < payCount; p++)
            {
                var payAmount = Math.Round(customer.Balance * (decimal)(0.2 + rng.NextDouble() * 0.4), 2);
                if (payAmount <= 0) break;
                payAmount = Math.Min(payAmount, customer.Balance);

                var payDate = now.AddDays(-rng.Next(1, 200));
                db.Payments.Add(new Payment
                {
                    PaymentNumber = $"TAH-{payDate.Year}-{paySeq++:D5}",
                    Type = PaymentType.Incoming,
                    Method = paymentMethods[rng.Next(paymentMethods.Length)],
                    PaymentDate = payDate,
                    Amount = payAmount,
                    CustomerId = customer.Id,
                    Notes = $"{customer.FullName} tahsilat",
                    IsActive = true,
                    CreatedAt = payDate,
                    CreatedBy = "sistem"
                });
                customer.Balance = Math.Round(customer.Balance - payAmount, 2);
            }
        }

        // ── 9. TEDARİKÇİ ÖDEMELERİ ──────────────────────────────────────────
        foreach (var supplier in suppliers.Where(s => s.Balance > 0))
        {
            var payCount = rng.Next(1, 3);
            for (int p = 0; p < payCount; p++)
            {
                var payAmount = Math.Round(supplier.Balance * (decimal)(0.3 + rng.NextDouble() * 0.4), 2);
                if (payAmount <= 0) break;
                payAmount = Math.Min(payAmount, supplier.Balance);

                var payDate = now.AddDays(-rng.Next(1, 200));
                db.Payments.Add(new Payment
                {
                    PaymentNumber = $"ODE-{payDate.Year}-{paySeq++:D5}",
                    Type = PaymentType.Outgoing,
                    Method = paymentMethods[rng.Next(paymentMethods.Length)],
                    PaymentDate = payDate,
                    Amount = payAmount,
                    SupplierId = supplier.Id,
                    Notes = $"{supplier.CompanyName} tedarikçi ödemesi",
                    IsActive = true,
                    CreatedAt = payDate,
                    CreatedBy = "sistem"
                });
                supplier.Balance = Math.Round(supplier.Balance - payAmount, 2);
            }
        }

        db.Customers.UpdateRange(customers);
        db.Suppliers.UpdateRange(suppliers);
        await db.SaveChangesAsync();
    }

    private static readonly string[] streets = [
        "Atatürk Cad.", "Cumhuriyet Cad.", "İstiklal Cad.", "Bağlar Sok.", "Çiçek Sok.",
        "Gül Sok.", "Lale Cad.", "Millet Cad.", "Vatan Cad.", "Hürriyet Cad.",
        "Ergenekon Cad.", "Fatih Cad.", "Selimiye Cad.", "Barış Mah.", "Yeniköy Sok."
    ];
}
