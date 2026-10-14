
const API_BASE_URL = 'https://localhost:7205/api';

export async function createJob(urls) {
    const response = await fetch(`${API_BASE_URL}/jobs`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ urls })
    });

    if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || 'Не удалось создать задачу');
    }

    return await response.json();
}

export async function createLogAnalysisJob(sourceText) {
    const response = await fetch(`${API_BASE_URL}/jobs/log-analysis`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ sourceText })
    });

    if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || 'Не удалось отправить логи');
    }

    return await response.json();
}

export async function getJob(jobId) {
    const response = await fetch(`${API_BASE_URL}/jobs/${jobId}`);

    if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || 'Не удалось получить задачу');
    }

    return await response.json();
}
