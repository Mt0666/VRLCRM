/*
 * VRLScanner — html5-qrcode için ortak, iOS-uyumlu barkod/QR tarayıcı sarmalayıcısı.
 *
 * Platform farkları:
 *   Android Chrome : zoom + focusMode 'continuous' destekleniyor → 800ms sonra uygulanır.
 *   iOS Safari     : focusMode desteklenmiyor, zoom iOS 17+ destekleniyor → 2250ms sonra uygulanır.
 *                    aspectRatio taramayı kesiyor → hiç kullanılmıyor.
 *
 * Kullanım:
 *   const scanner = VRLScanner.create({
 *     readerId, wrapperId, toggleBtnId, stopBtnId, torchBtnId,
 *     messageId, idleLabel, runningLabel,
 *     onDetected: function (text, scanner) { ... }
 *   });
 */
(function (window, document) {
  'use strict';

  var isIOS = /iPad|iPhone|iPod/.test(navigator.userAgent) ||
              (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);

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
    var startErrorMessage = opts.startErrorMessage || 'Kamera başlatılamadı. Tarayıcı izinlerini kontrol edin.';
    var onDetected = typeof opts.onDetected === 'function' ? opts.onDetected : function () {};

    var html5QrCode = null;
    var running = false;
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

    /*
     * Her iki platform için kamera kısıtlamalarını uygular.
     *  - zoom   : hem Android hem iOS'ta okunabilirliği artırıyor (kullanıcı onayladı).
     *  - focusMode 'continuous' : yalnızca Android'de destekleniyor.
     *  - torch  : destekleniyorsa düğmeyi göster.
     *
     * Önce html5QrCode.getRunningTrackCapabilities() dene (kütüphane API'si),
     * başarısız olursa doğrudan track.getCapabilities() ile aynı şeyi yap.
     */
    function applyTrackConstraints() {
      if (!running) return;

      var caps = null;
      var usingLibApi = false;

      // Yöntem 1: kütüphane API'si
      if (html5QrCode && typeof html5QrCode.getRunningTrackCapabilities === 'function') {
        try { caps = html5QrCode.getRunningTrackCapabilities(); usingLibApi = true; } catch (e) {}
      }

      // Yöntem 2: doğrudan track
      if (!caps) {
        var track = getTrack();
        if (track && typeof track.getCapabilities === 'function') {
          try { caps = track.getCapabilities(); } catch (e) {}
        }
      }

      if (!caps) return;

      // Torch düğmesi
      if (torchBtn && caps.torch) torchBtn.style.display = '';

      // Uygulanacak constraint'ler
      var advanced = [];

      // Zoom — Android ve iOS (17+) destekler, barkod okunabilirliğini artırıyor
      if (caps.zoom && caps.zoom.max) {
        var targetZoom = Math.min(caps.zoom.max, 2);
        advanced.push({ zoom: targetZoom });
      }

      // focusMode: 'continuous' — yalnızca Android
      if (!isIOS && caps.focusMode && Array.isArray(caps.focusMode) &&
          caps.focusMode.indexOf('continuous') !== -1) {
        advanced.push({ focusMode: 'continuous' });
      }

      if (!advanced.length) return;

      if (usingLibApi) {
        // html5QrCode.applyVideoConstraints API'si
        try {
          html5QrCode.applyVideoConstraints({ advanced: advanced });
        } catch (e) {}
      } else {
        // Doğrudan track API'si
        var t = getTrack();
        if (t) { try { t.applyConstraints({ advanced: advanced }); } catch (e) {} }
      }
    }

    function onStarted() {
      // Android daha hızlı hazır, iOS daha uzun süre beklemesi gerekiyor
      setTimeout(applyTrackConstraints, isIOS ? 2250 : 800);
    }

    function handleDecoded(decodedText) {
      stop().then(function () { onDetected(decodedText, api); });
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
      if (window.isSecureContext === false) {
        setMessage('Kamera yalnızca güvenli bağlantıda (HTTPS) çalışır.');
        return;
      }

      if (wrapperEl) wrapperEl.style.display = 'block';
      running = true;
      if (toggleBtn) toggleBtn.innerHTML = runningLabel;
      setMessage('');

      if (!html5QrCode) html5QrCode = new Html5Qrcode(readerId);

      var config = {
        fps: 15,
        qrbox: wideBox,
        // aspectRatio iOS'ta taramayı kesiyor — hiç kullanılmıyor
        videoConstraints: { facingMode: 'environment', width: { ideal: 1280 }, height: { ideal: 720 } }
      };

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

    var api = { start: start, stop: stop, toggle: toggle, isRunning: function () { return running; } };
    return api;
  }

  window.VRLScanner = { create: create };
})(window, document);
