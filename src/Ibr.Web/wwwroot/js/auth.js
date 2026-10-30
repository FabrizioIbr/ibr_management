window.ibrAuth = {
    login: async (email, password) => {
        const response = await fetch('/api/auth/login', {
            method: 'POST',
            credentials: 'include',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password })
        });

        if (response.ok) {
            return { ok: true };
        }

        let message = response.status === 401
            ? 'El correo corporativo o la contraseña no son válidos.'
            : response.status === 403
                ? 'Tu cuenta no tiene permiso para iniciar sesión.'
                : 'No se pudo iniciar sesión. Inténtalo nuevamente.';

        try {
            const contentType = response.headers.get('content-type') || '';
            if (contentType.includes('application/json')) {
                const data = await response.json();
                if (typeof data?.message === 'string' && data.message.trim() !== '') {
                    message = data.message;
                } else if (typeof data?.error === 'string' && data.error.trim() !== '') {
                    message = data.error;
                } else if (response.status !== 401 && typeof data?.title === 'string' && data.title.trim() !== '') {
                    message = data.title;
                }
            } else {
                const text = await response.text();
                if (text && text.trim() !== '') {
                    message = text.trim();
                }
            }
        } catch {
            // Mantener mensaje por defecto
        }

        return { ok: false, message };
    },

    logout: async () => {
        await fetch('/api/auth/logout', {
            method: 'POST',
            credentials: 'include'
        });
    }
};
