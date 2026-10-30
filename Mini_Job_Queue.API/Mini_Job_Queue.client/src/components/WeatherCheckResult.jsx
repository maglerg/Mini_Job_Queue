import React from 'react';

function formatDate(value) {
    return value ? new Date(value).toLocaleString() : '—';
}

function WeatherCheckResult({ result }) {
    if (!result) {
        return null;
    }

    return (
        <section className="weather-panel weather-check-result">
            <h2>
                Текущая погода: {result.locationName}
            </h2>

            <p className="weather-check-time">
                Данные получены: {formatDate(result.recordedAt)}
            </p>

            <div className="weather-latest-grid">
                <article>
                    <span>Температура</span>
                    <strong>
                        {result.temperatureCelsius ?? '—'} °C
                    </strong>
                </article>

                <article>
                    <span>Влажность</span>
                    <strong>
                        {result.relativeHumidityPercent != null
                            ? `${result.relativeHumidityPercent}%`
                            : '—'}
                    </strong>
                </article>

                <article>
                    <span>Осадки</span>
                    <strong>
                        {result.precipitationMm != null
                            ? `${result.precipitationMm} мм`
                            : '—'}
                    </strong>
                </article>

                <article>
                    <span>Скорость ветра</span>
                    <strong>
                        {result.windSpeedKmh != null
                            ? `${result.windSpeedKmh} км/ч`
                            : '—'}
                    </strong>
                </article>
            </div>
        </section>
    );
}

export default WeatherCheckResult;