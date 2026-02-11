// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Сохраняем и восстанавливаем состояние навигационного меню при перезагрузке страницы
(function () {
    var STORAGE_KEY = 'quizapp-navbar-expanded';
    var collapseEl = document.querySelector('.navbar-collapse');
    var toggler = document.querySelector('.navbar-toggler');
    
    if (!collapseEl || !toggler) return;
    
    function isExpanded() {
        return collapseEl.classList.contains('show');
    }
    
    function saveState() {
        try {
            localStorage.setItem(STORAGE_KEY, isExpanded() ? '1' : '0');
        } catch (e) {}
    }
    
    function restoreState() {
        try {
            if (localStorage.getItem(STORAGE_KEY) === '1' && toggler.offsetParent !== null) {
                var bsCollapse = bootstrap.Collapse.getInstance(collapseEl) || new bootstrap.Collapse(collapseEl, { toggle: false });
                bsCollapse.show();
                toggler.setAttribute('aria-expanded', 'true');
            }
        } catch (e) {}
    }
    
    collapseEl.addEventListener('show.bs.collapse', saveState);
    collapseEl.addEventListener('hide.bs.collapse', saveState);
    
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', restoreState);
    } else {
        restoreState();
    }
})();
