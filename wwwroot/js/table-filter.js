(function () {
  function initTableFilter(table) {
    const tbody = table.querySelector('tbody');
    if (!tbody) return;

    const toolbar = table.closest('.card')?.querySelector('.table-toolbar');
    const searchInput = toolbar?.querySelector('.js-table-search');
    const pageSizeSelect = toolbar?.querySelector('.js-table-page-size');
    const rows = Array.from(tbody.querySelectorAll('tr')).filter(row => !row.querySelector('td[colspan]'));
    let filteredRows = rows.slice();
    let currentPage = 1;

    const tableResponsive = table.closest('.table-responsive');
    let paginationContainer = null;
    if (tableResponsive) {
        if (!tableResponsive.nextElementSibling?.classList.contains('table-pagination-container')) {
             paginationContainer = document.createElement('div');
             paginationContainer.className = 'table-pagination-container d-flex flex-wrap justify-content-between align-items-center px-4 py-3 border-top gap-3';
             tableResponsive.parentNode.insertBefore(paginationContainer, tableResponsive.nextSibling);
        } else {
             paginationContainer = tableResponsive.nextElementSibling;
        }
    }

    // Hide the old info element if it exists from previous logic
    const oldInfo = toolbar?.querySelector('.js-table-info');
    if (oldInfo) oldInfo.style.display = 'none';

    function render() {
      const pageSize = parseInt(pageSizeSelect?.value || '25', 10);
      const totalPages = Math.max(1, Math.ceil(filteredRows.length / pageSize));
      if (currentPage > totalPages) currentPage = totalPages;
      if (currentPage < 1) currentPage = 1;

      rows.forEach(row => { row.style.display = 'none'; });

      const start = (currentPage - 1) * pageSize;
      filteredRows.slice(start, start + pageSize).forEach(row => {
        row.style.display = '';
      });

      if (paginationContainer) {
        let infoHtml = '';
        if (filteredRows.length === 0) {
          infoHtml = '<div class="text-body-secondary small">Kayıt bulunamadı.</div>';
        } else {
          const from = start + 1;
          const to = Math.min(start + pageSize, filteredRows.length);
          infoHtml = `<div class="text-body-secondary small">${filteredRows.length} kayıttan <strong>${from}-${to}</strong> arası gösteriliyor</div>`;
        }

        let paginationHtml = '';
        if (totalPages > 1) {
          paginationHtml += `<ul class="pagination pagination-sm mb-0">`;
          
          paginationHtml += `<li class="page-item ${currentPage === 1 ? 'disabled' : ''}">
                               <a class="page-link js-page-link" href="javascript:void(0);" data-page="${currentPage - 1}">Önceki</a>
                             </li>`;
          
          for (let i = 1; i <= totalPages; i++) {
             if (i === 1 || i === totalPages || (i >= currentPage - 1 && i <= currentPage + 1)) {
                 paginationHtml += `<li class="page-item ${i === currentPage ? 'active' : ''}">
                                      <a class="page-link js-page-link" href="javascript:void(0);" data-page="${i}">${i}</a>
                                    </li>`;
             } else if (i === currentPage - 2 || i === currentPage + 2) {
                 paginationHtml += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
             }
          }

          paginationHtml += `<li class="page-item ${currentPage === totalPages ? 'disabled' : ''}">
                               <a class="page-link js-page-link" href="javascript:void(0);" data-page="${currentPage + 1}">Sonraki</a>
                             </li>`;
                             
          paginationHtml += `</ul>`;
        }

        paginationContainer.innerHTML = `${infoHtml}<nav>${paginationHtml}</nav>`;

        paginationContainer.querySelectorAll('.js-page-link').forEach(link => {
            link.addEventListener('click', function(e) {
                e.preventDefault();
                const page = parseInt(this.getAttribute('data-page'), 10);
                if(page && page !== currentPage && page >= 1 && page <= totalPages) {
                    currentPage = page;
                    render();
                }
            });
        });
      }
    }

    function applySearch() {
      const term = (searchInput?.value || '').trim().toLowerCase();
      filteredRows = rows.filter(row => row.textContent.toLowerCase().includes(term));
      currentPage = 1;
      render();
    }

    searchInput?.addEventListener('input', applySearch);
    pageSizeSelect?.addEventListener('change', () => {
      currentPage = 1;
      render();
    });

    render();
  }

  document.querySelectorAll('table.js-data-table').forEach(initTableFilter);

  document.querySelectorAll('.js-flash-alert').forEach(alertEl => {
    setTimeout(() => {
      const closeBtn = alertEl.querySelector('.btn-close');
      if (closeBtn) closeBtn.click();
    }, 5000);
  });
})();
