
import React, { useState } from 'react';

function WeatherForm({ onSubmit, isSubmitting }) {
    const [locationName, setLocationName] = useState('');
    const [intervalMinutes, setIntervalMinutes] = useState(5);

    const handleSubmit = async (event) => {
        event.preventDefault();

        if (!locationName.trim()) {
            return;
        }

        if (!Number.isInteger(Number(intervalMinutes)) ||
            Number(intervalMinutes) < 1) {
            return;
        }

        await onSubmit({
            locationName: locationName.trim(),
            intervalMinutes: Number(intervalMinutes)
        });

        // Не очищаем форму автоматически при ошибке.
        setLocationName('');
        setIntervalMinutes(5);
    };

    return (
        <section className="weather-form-section">
            <h2>Новое отслеживание</h2>

            <p>
                Выбери город и настрой, как часто сервер
                будет сохранять погодные данные.
            </p>

            <form className="weather-form" onSubmit={handleSubmit}>
                <label htmlFor="weather-city">
                    Город или населённый пункт
                </label>

                <input
                    id="weather-city"
                    type="text"
                    value={locationName}
                    onChange={(event) =>
                        setLocationName(event.target.value)
                    }
                    placeholder="Например, Amsterdam"
                    maxLength={150}
                    required
                    disabled={isSubmitting}
                />

                <label htmlFor="weather-interval">
                    Интервал сбора данных
                </label>

                <div className="weather-interval">
                    <input
                        id="weather-interval"
                        type="number"
                        min="1"
                        step="1"
                        value={intervalMinutes}
                        onChange={(event) =>
                            setIntervalMinutes(event.target.value)
                        }
                        required
                        disabled={isSubmitting}
                    />

                    <span>мин.</span>
                </div>

                <p className="input-hint">
                    Например, 5 — новый замер примерно каждые пять минут.
                </p>

                <button
                    type="submit"
                    disabled={
                        isSubmitting ||
                        !locationName.trim() ||
                        !Number.isInteger(Number(intervalMinutes)) ||
                        Number(intervalMinutes) < 1
                    }
                >
                    {isSubmitting
                        ? 'Создание...'
                        : 'Начать отслеживание'}
                </button>
            </form>
        </section>
    );
}

export default WeatherForm;
