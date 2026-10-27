# Ofis Offline HTTPS — Kurulum Rehberi

İnternet varken erişim **Cloudflare Tunnel** üzerinden çalışır. İnternet kesildiğinde
ofisteki telefon/cihazlar aynı adrese (`crm.metevarol.com.tr`) **yerel ağ üzerinden,
geçerli HTTPS sertifikasıyla** erişmeye devam eder. Telefonlara hiçbir sertifika kurulmaz.

## Nasıl çalışır?

- **Caddy** sunucuda her zaman açıktır; geçerli Let's Encrypt sertifikasıyla 443'te HTTPS sunar.
- **Yerel DNS (CoreDNS)** `crm.metevarol.com.tr` adını sunucunun LAN IP'sine çözer; diğer
  tüm isimleri internete forward eder.
- **Cloudflare Tunnel** yalnızca ofis **dışından** erişim içindir.

| Durum | İsim çözümleme | Erişim yolu |
|---|---|---|
| İnternet VAR (ofis içi) | yerel DNS → LAN IP | doğrudan Caddy (hızlı) |
| İnternet YOK (ofis içi) | yerel DNS → LAN IP | doğrudan Caddy (çalışır) |
| Ofis dışı | Cloudflare | Tunnel |
| Sunucu kapalı | ikincil DNS (1.1.1.1) | internet normal, CRM zaten yok |

## Kurulum (her müşteride bir kez)

### 1. Sunucu
- Sunucuya **sabit LAN IP** ver (örn. `192.168.1.50`).
- Docker Desktop kurulu + "sign-in'de başlat" açık.

### 2. Cloudflare (bir kez)
- Panel → My Profile → API Tokens → Create Token
- Yetki: **Zone → DNS → Edit**, sadece `metevarol.com.tr` zonu.
- (Mevcut Tunnel ayarına dokunma.)

### 3. `.env` doldur
```
CRM_HOSTNAME=crm.metevarol.com.tr
LOCAL_LAN_IP=192.168.1.50
CLOUDFLARE_API_TOKEN=...        # Tunnel token'dan AYRI
ACME_EMAIL=admin@vrlcrm.local
CLOUDFLARE_TUNNEL_TOKEN=...     # zaten dolu
```

### 4. Sertifika al + başlat
```powershell
powershell -ExecutionPolicy Bypass -File scripts\windows\obtain-letsencrypt.ps1
docker compose --profile local-https --profile tunnel up -d
# veya: scripts\windows\start-vrlcrm.ps1  (sertifika varsa local-https'i otomatik ekler)
```
certbot, Cloudflare'de geçici bir TXT kaydıyla doğrular (otomatik ekler/siler), sertifikayı
indirir ve `caddy/runtime/Caddyfile`'ı üretir. İnternet yalnızca bu adımda gerekir.

### 5. Router — tek ayar
Router DHCP ayarları:
- **Birincil DNS = sunucu IP** (`LOCAL_LAN_IP`)
- **İkincil DNS = 1.1.1.1**

Telefonların Wi-Fi'ını bir kez kapat/aç (yeni DNS'i alsınlar).

## Doğrulama
1. **Online:** telefon Wi-Fi'da → `https://crm.metevarol.com.tr` → yeşil kilit.
2. **Offline:** modemin WAN/internet bağlantısını çek → aynı adres → hâlâ açılmalı, yeşil kilit.
3. **Ofis dışı:** mobil veri → aynı adres → Cloudflare tunnel üzerinden açılmalı.

## Sertifika yenileme
Let's Encrypt sertifikası 90 gün geçerli. İnternet varken aylık bir zamanlanmış görevle
`obtain-letsencrypt.ps1` çalıştırılması önerilir (yeniler + Caddyfile'ı günceller).
Telefon tarafında hiçbir işlem gerekmez.

## Notlar
- `.env`, `certs/`, `caddy/runtime/` git'e gitmez (`.gitignore`).
- Yerel HTTPS yalnızca `local-https` profili ile çalışır; sertifika yoksa `start-vrlcrm.ps1`
  Caddy'yi hiç başlatmaz (hata üretmez).
