# VRLCRM Öncelikli Geliştirme Planı

Bu analiz, projenin mimari bütünlüğünü sağlamak ve CRM olarak temel işlevlerini tamamlamak adına **öncelik sırasına göre** hazırlanmıştır.

## 1. Dashboard (Ana Ekran) Fonksiyonelleştirmesi
Şu anki Dashboard statik bir yapıya (template görünümüne) sahip. CRM sisteminin kalbi burasıdır.
- **Yapılacaklar:** 
  - Günlük/Aylık Satış Özeti, Bekleyen Siparişler ve Toplam Alacak/Verecek (Müşteri/Tedarikçi bakiyeleri) gibi gerçek verilerin Controller üzerinden ViewModel ile View'a taşınması.
  - "Kritik Stok Uyarısı" widget'ının eklenmesi.
  - Materio içindeki ApexCharts veya benzeri bir kütüphane ile Satış Grafiği (Son 6 Ay) oluşturulması.

## 2. Mimari İyileştirme: UsersController ve Application Katmanı
Şu anda `UsersController` (Personel Yönetimi), Identity `UserManager`'ı doğrudan Presentation (Web) katmanında kullanıyor. Bu, projenin Clean Architecture yapısına (örneğin `CustomerService`, `OrderService` gibi) tam uymuyor.
- **Yapılacaklar:**
  - `VRLCRM.Application` katmanında bir `IUserService` ve `VRLCRM.Infrastructure` katmanında `UserService` oluşturularak `UserManager` bağımlılığının buraya taşınması.
  - Controller'ın sadece bu servisi çağırması.

## 3. Sistem Denetim İzi (Audit Logging)
CRM sistemlerinde verinin kim tarafından değiştirildiğini bilmek güvenlik ve takip için şarttır.
- **Yapılacaklar:**
  - `ApplicationDbContext` içindeki `SaveChangesAsync` metodunun override edilmesi.
  - `BaseEntity`'ye `CreatedBy` ve `UpdatedBy` (string/UserId) alanlarının eklenmesi.
  - Sisteme giriş yapmış olan kullanıcının ID'sinin (veya Email'inin) `HttpContextAccessor` üzerinden alınıp bu alanlara otomatik yazılması.

## 4. Kullanıcı Deneyimi (UI/UX) ve Hata Yönetimi
Projede hatalar genellikle `ModelState.AddModelError` ile forma geri döndürülüyor veya `TempData["SuccessMessage"]` ile iletiliyor ancak bunların frontend'de standart bir gösterimi tam olarak oturmamış olabilir.
- **Yapılacaklar:**
  - `TempData` mesajlarını okuyup otomatik olarak bir **Toast/Snackbar (örn: SweetAlert2 veya Materio Toast)** gösteren global bir partial view (`_Alerts.cshtml`) oluşturulması.
  - Silme işlemleri için standart (SweetAlert tabanlı) onay pencerelerinin (Confirmation Dialog) her yere entegre edilmesi.
  - Global Exception Handler'ın (`Handlers/GlobalExceptionHandler.cs`) 500 hataları durumunda kullanıcı dostu bir hata sayfasına yönlendirme yapısının test edilip iyileştirilmesi.

## 5. Raporlama Modülü
Sipariş ve faturaların PDF/Excel çıktıları var, ancak sistem genelini kapsayan filtreli listeler yok.
- **Yapılacaklar:**
  - "Raporlar" adında yeni bir menü ve Controller oluşturulması.
  - Tarih aralığına göre **Satış Raporu** (Hangi müşteri ne kadarlık ürün aldı).
  - **Stok Durum Raporu** (Hangi üründen elimizde ne kadar kaldı, maliyeti nedir).

---

**Benim Önerim:** En kritik ve gözle görülür etkiyi yaratacağı için **1. Dashboard Fonksiyonelleştirmesi** ve **3. Sistem Denetim İzi (Audit Logging)** adımlarından başlamak olacaktır. 

Hangi adımdan (veya adımlardan) ilerleyelim?
