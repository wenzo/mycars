'use strict';
(function () {
    const overlay = document.createElement('div');
    overlay.id = 'sidebarOverlay';
    overlay.className = 'sidebar-overlay';

    const sidebar = document.createElement('aside');
    sidebar.className = 'sidebar';
    sidebar.id = 'sidebar';
    sidebar.setAttribute('role', 'navigation');
    sidebar.setAttribute('aria-label', 'Menu principale');
    sidebar.innerHTML = `
    <div class="sidebar-brand">
        <svg width="28" height="28" viewBox="0 0 40 40" fill="none" aria-hidden="true">
            <rect width="40" height="40" rx="8" fill="var(--red)"/>
            <path d="M8 26 L14 14 L20 22 L26 14 L32 26" stroke="#fff" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" fill="none"/>
            <circle cx="20" cy="12" r="3" fill="#fff"/>
        </svg>
        <span class="sidebar-brand-name">MyCars</span>
        <span class="sidebar-brand-sub">Admin</span>
    </div>
    <nav class="sidebar-nav">
        <a class="nav-item" data-page="index.html" href="/admin/index.html">
            <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/></svg>
            Dashboard
        </a>
        <a class="nav-item" data-page="veicoli.html" href="/admin/veicoli.html">
            <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M5 17H3a2 2 0 0 1-2-2V9a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v6a2 2 0 0 1-2 2h-2m-4 0H9m4 0v2a1 1 0 0 1-1 1H10a1 1 0 0 1-1-1v-2m4 0H9"/><circle cx="7.5" cy="17" r="1.5"/><circle cx="16.5" cy="17" r="1.5"/></svg>
            Veicoli
            <span class="nav-badge nav-badge-blue" id="vehiclesBadge"></span>
        </a>
        <a class="nav-item" data-page="news.html" href="/admin/news.html">
            <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M4 22h16a2 2 0 0 0 2-2V4a2 2 0 0 0-2-2H8a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2z"/><path d="M16 2v4"/><path d="M8 2v4"/><path d="M3 10h18"/></svg>
            News &amp; Promozioni
            <span class="nav-badge nav-badge-blue" id="newsBadge"></span>
        </a>
        <a class="nav-item" data-page="lead.html" href="/admin/lead.html">
            <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/></svg>
            Lead &amp; Richieste
            <span class="nav-badge" id="leadBadge"></span>
        </a>
        <a class="nav-item" data-page="test-drive.html" href="/admin/test-drive.html">
            <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
            Test Drive
            <span class="nav-badge nav-badge-red" id="testDriveBadge"></span>
        </a>
        <a class="nav-item" data-page="push.html" href="/admin/push.html">
            <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/><path d="M13.73 21a2 2 0 0 1-3.46 0"/></svg>
            Notifiche Push
        </a>
        <a class="nav-item" data-page="noleggi.html" href="/admin/noleggi.html">
            <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"/><circle cx="12" cy="10" r="3"/></svg>
            Noleggi
            <span class="nav-badge nav-badge-yellow" id="noleggiBadge"></span>
        </a>
        <div class="nav-divider"></div>
        <a class="nav-item" data-page="sedi.html" href="/admin/sedi.html">
            <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>
            Sedi &amp; Reparti
        </a>
        <div class="nav-divider"></div>
        <a class="nav-item" data-page="impostazioni.html" href="/admin/impostazioni.html">
            <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/></svg>
            Impostazioni
        </a>
        <form method="post" action="/api/admin/logout" style="margin-top:auto">
            <button type="submit" class="nav-item nav-item-logout" style="width:100%;background:none;border:none;cursor:pointer;text-align:left">
                <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" y1="12" x2="9" y2="12"/></svg>
                Esci
            </button>
        </form>
    </nav>
    <div class="sidebar-footer">
        <div class="sidebar-user">
            <div class="sidebar-user-avatar" id="userAvatar">A</div>
            <div class="sidebar-user-info">
                <span class="sidebar-user-name" id="userName">Admin</span>
                <span class="sidebar-user-role">Amministratore</span>
            </div>
        </div>
    </div>`;

    document.body.appendChild(overlay);
    document.body.appendChild(sidebar);
})();
