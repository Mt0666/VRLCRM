window.FormDraftStorage = (function () {
  function save(key, data) {
    try {
      sessionStorage.setItem(key, JSON.stringify(data));
    } catch (e) { /* ignore quota errors */ }
  }

  function load(key) {
    try {
      var raw = sessionStorage.getItem(key);
      return raw ? JSON.parse(raw) : null;
    } catch (e) {
      return null;
    }
  }

  function clear(key) {
    sessionStorage.removeItem(key);
  }

  function bindFormFields(key, formSelector, fieldNames) {
    var form = document.querySelector(formSelector);
    if (!form) return;

    var draft = load(key);
    if (draft && draft.fields) {
      fieldNames.forEach(function (name) {
        var el = form.querySelector('[name="' + name + '"]');
        if (el && draft.fields[name] !== undefined && draft.fields[name] !== null) {
          el.value = draft.fields[name];
        }
      });
    }

    function persistFields() {
      var fields = {};
      fieldNames.forEach(function (name) {
        var el = form.querySelector('[name="' + name + '"]');
        if (el) fields[name] = el.value;
      });
      var existing = load(key) || {};
      existing.fields = fields;
      save(key, existing);
    }

    fieldNames.forEach(function (name) {
      var el = form.querySelector('[name="' + name + '"]');
      if (el) {
        el.addEventListener('change', persistFields);
        el.addEventListener('input', persistFields);
      }
    });
  }

  return { save: save, load: load, clear: clear, bindFormFields: bindFormFields };
})();
