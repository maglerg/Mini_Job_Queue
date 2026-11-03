
const API_URL = import.meta.env.VITE_API_BASE_URL + '/automation-rules';

async function readResponse(response, fallbackMessage) {
    const data = await response.json().catch(() => null);

    if (!response.ok) {
        throw new Error(
            data?.error ?? fallbackMessage
        );
    }

    return data;
}

export async function createAutomationRule({
    intervalSeconds,
    urls
}) {
    const response = await fetch(API_URL, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            type: 0,
            intervalSeconds: Number(intervalSeconds),
            urls
        })
    });

    return readResponse(
        response,
        'Не удалось создать автоматизацию.'
    );
}

export async function stopAutomationRule(ruleId) {
    const response = await fetch(
        `${API_URL}/${ruleId}/stop`,
        {
            method: 'POST'
        }
    );

    return readResponse(
        response,
        'Не удалось остановить автоматизацию.'
    );
}

export async function getAutomationRuns(ruleId) {
    const response = await fetch(
        `${API_URL}/${ruleId}/runs`
    );

    return readResponse(
        response,
        'Не удалось загрузить историю запусков.'
    );
}
