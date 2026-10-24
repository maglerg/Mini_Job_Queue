
const API_URL = 'https://localhost:7205/api/weather';

async function readResponse(response, fallbackMessage) {
    if (response.status === 204) {
        return null;
    }

    const data = await response.json().catch(() => null);

    if (!response.ok) {
        throw new Error(
            data?.message ??
            data?.error ??
            fallbackMessage
        );
    }

    return data;
}

// Создание отслеживания города.
export async function createWeatherTracking({
    locationName,
    intervalMinutes
}) {
    const response = await fetch(`${API_URL}/trackings`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            locationName,
            intervalMinutes: Number(intervalMinutes)
        })
    });

    return readResponse(
        response,
        'Не удалось создать отслеживание погоды.'
    );
}

// Получение всех отслеживаний.
export async function getWeatherTrackings() {
    const response = await fetch(`${API_URL}/trackings`);

    return readResponse(
        response,
        'Не удалось загрузить список отслеживаний.'
    );
}

// Остановка отслеживания.
export async function stopWeatherTracking(id) {
    const response = await fetch(
        `${API_URL}/trackings/${id}/stop`,
        { method: 'POST' }
    );

    return readResponse(
        response,
        'Не удалось остановить отслеживание.'
    );
}

// История замеров конкретного города.
export async function getWeatherHistory(id) {
    const response = await fetch(
        `${API_URL}/trackings/${id}/history`
    );

    return readResponse(
        response,
        'Не удалось загрузить историю погоды.'
    );
}
