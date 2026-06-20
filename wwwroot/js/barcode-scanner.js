/*
 * VRLScanner — html5-qrcode için ortak, iOS-uyumlu barkod/QR tarayıcı sarmalayıcısı.
 *
 * Neden bu modül:
 *   Tarayıcı kodu eskiden 5 view'a kopyalanmıştı ve iOS'ta kötü çalışıyordu.
 *   iOS'taki TÜM tarayıcılar (Chrome, Firefox, Edge dahil) zorunlu olarak WebKit
 *   kullanır — yani sorunlar Safari/WebKit limitleridir. Bu modül iOS'ta doğru
 *   davranan tek bir yapılandırmayı merkezîleştirir:
 *     - Yüksek çözünürlük videoConstraints (bare facingMode yerine) -> net çizgiler.
 *     - aspectRatio KULLANILMAZ (iOS'ta video gerilmesinin/garip şekilli alanın sebebi).
 *     - focusMode / torch yalnızca cihaz capability bildirirse uygulanır (Android);
 *       iOS'ta desteklenmediği için atlanır (eski kör applyVideoConstraints no-op'tu).
 *     - Aynı kodu 2 kez okuma teyidi (yanlış okumayı azaltır).
 *
 * Kullanım:
 *   const scanner = VRLScanner.create({
 *     readerId: 'qr-reader',          // zorunlu — Html5Qrcode'un mount edileceği div id
 *     wrapperId: 'scannerWrapper',    // açılıp kapanan kapsayıcı (display toggle)
 *     toggleBtnId: 'scannerBtn',      // aç/kapa butonu (ikon/etiket otomatik değişir)
 *     stopBtnId: 'stopScannerBtn',
 *     torchBtnId: 'torchBtn',         // yalnız cihaz destekliyorsa görünür olur
 *     messageId: 'productMessage',    // başlatma hatası mesajı buraya yazılır
 *     idleLabel: '...',               // toggle butonu kapalıyken içeriği
 *     runningLabel: '...',            // toggle butonu açıkken içeriği
 *     onDetected: function (text, scanner) { ... }  // tarama başarılı (kamera durdurulur)
 *   });
 *   // scanner.start() / scanner.stop() / scanner.toggle() / scanner.isRunning()
 */
(function (window, document) {
  'use strict';

  // 1D barkodlar için geniş, görüntüye göre ölçeklenen tarama kutusu.
  function wideBox(viewfinderWidth, viewfinderHeight) {
    var side = Math.round(Math.min(viewfinderWidth, viewfinderHeight) * 0.72);
    return { width: side, height: Math.round(side * 0.48) };
  }

  function create(opts) {
    opts = opts || {};
    var readerId = opts.readerId;
    if (!readerId) { throw new Error('VRLScanner.create: readerId zorunlu'); }

    var byId = function (id) { return id ? document.getElementById(id) : null; };
    var wrapperEl = byId(opts.wrapperId);
    var toggleBtn = byId(opts.toggleBtnId);
    var stopBtn   = byId(opts.stopBtnId);
    var torchBtn  = byId(opts.torchBtnId);
    var msgEl     = byId(opts.messageId);

    var idleLabel    = opts.idleLabel    || '<i class="ri-camera-line me-1"></i>Barkod Tara';
    var runningLabel = opts.runningLabel || '<i class="ri-camera-off-line me-1"></i>Kamerayı Kapat';
    var doubleConfirm = opts.doubleConfirm !== false; // varsayılan: açık
    var startErrorMessage = opts.startErrorMessage || 'Kamera başlatılamadı. Tarayıcı izinlerini kontrol edin.';
    var onDetected = typeof opts.onDetected === 'function' ? opts.onDetected : function () {};

    var html5QrCode = null;
    var running = false;
    var pendingCode = null;
    var pendingHits = 0;
    var torchOn = false;

    function setMessage(text) {
      if (!msgEl) return;
      msgEl.textContent = text || '';
      msgEl.style.display = text ? '' : 'none';
    }

    function getTrack() {
      var video = document.querySelector('#' + readerId + ' video');
      if (!video || !video.srcObject || !video.srcObject.getVideoTracks) return null;
      var tracks = video.srcObject.getVideoTracks();
      return tracks && tracks.length ? tracks[0] : null;
    }

    // Kamera başladıktan sonra: yalnız cihaz destekliyorsa torch butonu göster ve
    // sürekli (continuous) odağı iste. iOS bunları desteklemez -> sessizce atlanır.
    function setupTrackFeatures() {
      if (!running) return;
      var track = getTrack();
      if (!track || !track.getCapabilities) return;
      var caps = {};
      try { caps = track.getCapabilities() || {}; } catch (e) { caps = {}; }

      if (torchBtn && caps.torch) {
        torchBtn.style.display = '';
      }
      if (caps.focusMode && caps.focusMode.indexOf && caps.focusMode.indexOf('continuous') !== -1) {
        try { track.applyConstraints({ advanced: [{ focusMode: 'continuous' }] }); } catch (e) {}
      }
    }

    function handleDecoded(decodedText) {
      if (doubleConfirm) {
        if (decodedText !== pendingCode) { pendingCode = decodedText; pendingHits = 1; return; }
        if (++pendingHits < 2) return;
      }
      pendingCode = null; pendingHits = 0;
      stop().then(function () { onDetected(decodedText, api); });
    }

    function onStarted() {
      setTimeout(setupTrackFeatures, 800);
    }

    function onStartError(err) {
      if (window.console) console.warn('[VRLScanner] kamera başlatılamadı:', err);
      html5QrCode = null;
      running = false;
      if (wrapperEl) wrapperEl.style.display = 'none';
      if (toggleBtn) toggleBtn.innerHTML = idleLabel;

      var name = (err && (err.name || err.message)) ? String(err.name || err.message) : '';
      if (/NotAllowed|Permission|Denied/i.test(name)) {
        setMessage('Kamera izni reddedildi. Tarayıcı ayarlarından bu site için kamera iznini açın.');
      } else if (/NotFound|Overconstrained|Devices/i.test(name)) {
        setMessage('Uygun arka kamera bulunamadı.');
      } else if (/NotReadable|TrackStart/i.test(name)) {
        setMessage('Kameraya erişilemedi. Başka bir uygulama kamerayı kullanıyor olabilir.');
      } else {
        setMessage(startErrorMessage);
      }
    }

    function start() {
      if (running) return;
      if (typeof Html5Qrcode === 'undefined') { setMessage('Barkod okuyucu yüklenemedi.'); return; }
      // Kamera yalnızca güvenli bağlamda (HTTPS veya localhost) çalışır — iOS bunda katıdır.
      if (window.isSecureContext === false) {
        setMessage('Kamera yalnızca güvenli bağlantıda (HTTPS) çalışır.');
        return;
      }

      if (wrapperEl) wrapperEl.style.display = 'block';
      running = true;
      pendingCode = null; pendingHits = 0;
      if (toggleBtn) toggleBtn.innerHTML = runningLabel;
      setMessage('');

      if (!html5QrCode) html5QrCode = new Html5Qrcode(readerId);

      var config = {
        fps: 10,
        qrbox: wideBox,
        // Çözünürlük ilk argümana DEĞİL videoConstraints'e verilir: html5-qrcode'un ilk argümanı
        // tam 1 anahtar kabul eder (facingMode VEYA deviceId). Gerçek kamera kısıtları burada.
        // 'ideal' spec gereği asla reddedilmez -> daha net görüntü (iOS bulanıklık için ana kazanç).
        videoConstraints: { facingMode: 'environment', width: { ideal: 1280 }, height: { ideal: 720 } }
      };

      // İlk argüman tek anahtarlı olmalı; gerçek kısıtlar yukarıdaki videoConstraints'ten gelir.
      html5QrCode.start({ facingMode: 'environment' }, config, handleDecoded, function () {})
        .then(onStarted)
        .catch(onStartError);
    }

    function resetTorchButton() {
      if (!torchBtn) return;
      torchBtn.style.display = 'none';
      torchBtn.innerHTML = '<i class="ri-flashlight-line"></i>';
      torchBtn.classList.remove('btn-warning');
      torchBtn.classList.add('btn-outline-warning');
    }

    function stop() {
      pendingCode = null; pendingHits = 0;
      var p = Promise.resolve();
      if (html5QrCode && running) {
        if (torchOn) {
          var track = getTrack();
          if (track) { try { track.applyConstraints({ advanced: [{ torch: false }] }); } catch (e) {} }
          torchOn = false;
        }
        p = html5QrCode.stop().catch(function () {});
      }
      return p.then(function () {
        running = false;
        resetTorchButton();
        if (wrapperEl) wrapperEl.style.display = 'none';
        if (toggleBtn) toggleBtn.innerHTML = idleLabel;
      });
    }

    function toggle() { if (running) { stop(); } else { start(); } }

    if (toggleBtn) toggleBtn.addEventListener('click', toggle);
    if (stopBtn) stopBtn.addEventListener('click', function () { stop(); });
    if (torchBtn) {
      torchBtn.addEventListener('click', function () {
        var track = getTrack();
        if (!track) return;
        torchOn = !torchOn;
        var self = this;
        Promise.resolve(track.applyConstraints({ advanced: [{ torch: torchOn }] }))
          .then(function () {
            self.innerHTML = torchOn ? '<i class="ri-flashlight-fill"></i>' : '<i class="ri-flashlight-line"></i>';
            self.classList.toggle('btn-warning', torchOn);
            self.classList.toggle('btn-outline-warning', !torchOn);
          })
          .catch(function () { torchOn = !torchOn; });
      });
    }

    var api = {
      start: start,
      stop: stop,
      toggle: toggle,
      isRunning: function () { return running; }
    };
    return api;
  }

  window.VRLScanner = { create: create };
})(window, document);
