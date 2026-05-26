window.dnsLookup = {
    copyToClipboard: async (text) => {
        try {
            await navigator.clipboard.writeText(text);
            return true;
        } catch {
            return false;
        }
    },
    getHistory: () => {
        try {
            const data = localStorage.getItem('dns-lookup-history');
            return data ? JSON.parse(data) : [];
        } catch {
            return [];
        }
    },
    saveHistory: (history) => {
        try {
            localStorage.setItem('dns-lookup-history', JSON.stringify(history));
        } catch {}
    }
};

