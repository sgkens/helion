document.addEventListener('alpine:init', () => {
    Alpine.data('dashboard_rbac', () => ({
        permissions: {
            read: true,
            write: true,
            admin: false,
            schema: false,
            backup: true
        },
        togglePermission(perm) {
            this.permissions[perm] = !this.permissions[perm];
            console.log(`Permission ${perm} updated:`, this.permissions[perm]);
        }
    }));
});
