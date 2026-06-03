'use strict';
(function () {
    const overlay = document.createElement('div');
    overlay.id = 'sidebarOverlay';
    overlay.className = 'sidebar-overlay';

    const sidebar = document.createElement('aside');
    sidebar.className = 'sidebar';
    sidebar.id = 'sidebar';
    sidebar.innerHTML = `
    <div class="sidebar-brand">
        <svg width="28" height="28" viewBox="0 0 40 40" fill="none">
            <rect width="40" height="40" rx="8" fill="#6c3483"/>
            <path d="M8 26 L14 14 L20 22 L26 14 L32 26" stroke="#fff" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" fill="none"/>
            <circle cx="20" cy="12" r="3" fill="#fff"/>
        </svg>
        <span class="sidebar-brand-name">SuperAdmin</span>
    </div>
    <nav class="sidebar-nav">
        <a class="nav-item" href="#" id="navIscrizioni" data-section="iscrizioni">
            <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/></svg>
            Iscrizioni
            <span class="nav-badge" id="pendingBadge"></span>
        </a>
        <a class="nav-item active" href="#" id="navOperatori" data-section="operatori">
            <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/></svg>
            Operatori attivi
        </a>
        <a class="nav-item" href="#" id="navUtility" data-section="utility">
            <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M7 17m-2 0a2 2 0 1 0 4 0a2 2 0 1 0 -4 0"/><path d="M17 17m-2 0a2 2 0 1 0 4 0a2 2 0 1 0 -4 0"/><path d="M5 17h-2v-11a1 1 0 0 1 1 -1h9v6h-5l2 3h6l2 -3h1v5h-2"/><path d="M9 11v-6h3l3 6"/></svg>
            Marchi veicoli
        </a>
        <div style="margin-top:auto"></div>
        <form method="post" action="/api/superadmin/logout">
            <button type="submit" class="nav-item nav-item-logout" style="width:100%;background:none;border:none;cursor:pointer;text-align:left">
                <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" y1="12" x2="9" y2="12"/></svg>
                Esci
            </button>
        </form>
    </nav>`;

    document.body.appendChild(overlay);
    document.body.appendChild(sidebar);
})();
