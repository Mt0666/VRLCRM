/*
 * VRLScanner — html5-qrcode için ortak, iOS-uyumlu barkod/QR tarayıcı sarmalayıcısı.
 *
 * Platform farkları:
 *   Android Chrome : focusMode 'continuous' destekleniyor → 800ms sonra track üzerinden uygulanır.
 *   iOS Safari     : focusMode desteklenmiyor, zoom iOS 17+ destekleniyor.
 *                    aspectRatio taramayı kesiyor → kullanılmıyor.
 *                    2250ms sonra getRunningTrackCapabilities() ile zoom uygulanır.
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

    // Android: track üzerinden focusMode + torch kontrolü (800ms — dün çalışan yaklaşım)
    function setupAndroid() {
      if (!running) return;
      var track = getTrack();
      if (!track || !track.getCapabilities) return;
      var caps = {};
      try { caps = track.getCapabilities() || {}; } catch (e) { return; }

      if (torchBtn && caps.torch) torchBtn.style.display = '';

      if (caps.focusMode && caps.focusMode.indexOf('continuous') !== -1) {
        try { track.applyConstraints({ advanced: [{ focusMode: 'continuous' }] }); } catch (e) {}
      }
    }

    // iOS: getRunningTrackCapabilities() ile zoom (2250ms — iOS 17+ destekler)
    function setupIOS() {
      if (!running || !html5QrCode) return;
      try {
        var caps = html5QrCode.getRunningTrackCapabilities();
        var advanced = [];
        if (caps.zoom) advanced.push({ zoom: Math.min(caps.zoom.max || 2, 2) });
        if (caps.focusDistance) advanced.push({ focusDistance: 1 });
        if (advanced.length) {
          html5QrCode.applyVideoConstraints({ advanced: advanced });
        }
      } catch (e) {}

      // Torch (iOS 17.5+)
      try {
        var track = getTrack();
        var trackCaps = track && track.getCapabilities ? track.getCapabilities() : {};
        if (torchBtn && trackCaps.torch) torchBtn.style.display = '';
      } catch (e) {}
    }

    function onStarted() {
      if (isIOS) {
        setTimeout(setupIOS, 2250);
      } else {
        setTimeout(setupAndroid, 800);
      }
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
        // aspectRatio iOS'ta taramayı kesiyor — kullanılmıyor
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
