import React, { useState } from 'react';

function WeatherCheckForm({ onSubmit, isSubmitting }) {
    const [locationName, setLocationName] = useState('');

    const handleSubmit = async (event) => {
        event.preventDefault();

        if (!locationName.trim()) {
            return;
        }

        await onSubmit(locationName.trim());
    };

    return (
        <section className="weather-check-section">
            <h2>Разовая проверка погоды</h2>

            <p>
                Введи название города, чтобы получить актуальные
                погодные данные без создания автоматизации.
            </p>

            <form
                className="weather-check-form"
                onSubmit={handleSubmit}
            >
                <label htmlFor="weather-check-city">
                    Город или населённый пункт
                </label>

                <input
                    id="weather-check-city"
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

                <button
                    type="submit"
                    disabled={
                        isSubmitting || !locationName.trim()
                    }
                >
                    {isSubmitting
                        ? 'Проверка...'
                        : 'Проверить погоду'}
                </button>
            </form>
        </section>
    );
}

export default WeatherCheckForm;