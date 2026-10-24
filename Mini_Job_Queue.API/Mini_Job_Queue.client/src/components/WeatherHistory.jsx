
import React from 'react';

function formatDate(value) {
    return value ? new Date(value).toLocaleString() : '—';
}

function WeatherHistory({ tracking, records, isLoading }) {
    if (!tracking) {
        return (
            <section className="weather-panel">
                <h2>История погоды</h2>
                <p>Выбери город, чтобы посмотреть его замеры.</p>
            </section>
        );
    }

    const sortedRecords = [...records].sort(
        (a, b) =>
            new Date(b.recordedAt).getTime() -
            new Date(a.recordedAt).getTime()
    );

    return (
        <section className="weather-panel">
            <h2>История погоды: {tracking.locationName}</h2>

            {isLoading && <p>Загрузка истории...</p>}

            {!isLoading && sortedRecords.length === 0 && (
                <p>
                    Замеров пока нет. Первый появится после выполнения
                    фоновой задачи сбора погоды.
                </p>
            )}

            {!isLoading && sortedRecords.length > 0 && (
                <>
                    <div className="weather-latest-grid">
                        <article>
                            <span>Последняя температура</span>
                            <strong>
                                {sortedRecords[0].temperatureCelsius} °C
                            </strong>
                        </article>

                        <article>
                            <span>Влажность</span>
                            <strong>
                                {sortedRecords[0].relativeHumidityPercent != null
                                    ? `${sortedRecords[0].relativeHumidityPercent}%`
                                    : '—'}
                            </strong>
                        </article>

                        <article>
                            <span>Осадки</span>
                            <strong>
                                {sortedRecords[0].precipitationMm != null
                                    ? `${sortedRecords[0].precipitationMm} мм`
                                    : '—'}
                            </strong>
                        </article>

                        <article>
                            <span>Скорость ветра</span>
                            <strong>
                                {sortedRecords[0].windSpeedKmh != null
                                    ? `${sortedRecords[0].windSpeedKmh} км/ч`
                                    : '—'}
                            </strong>
                        </article>
                    </div>

                    <h3>Все замеры</h3>

                    <div className="weather-record-list">
                        {sortedRecords.map((record) => (
                            <article key={record.id}>
                                <div className="weather-record-heading">
                                    <strong>
                                        {record.temperatureCelsius} °C
                                    </strong>
                                    <span>
                                        {formatDate(record.recordedAt)}
                                    </span>
                                </div>

                                <p>
                                    Влажность:{' '}
                                    {record.relativeHumidityPercent != null
                                        ? `${record.relativeHumidityPercent}%`
                                        : '—'}
                                </p>

                                <p>
                                    Осадки:{' '}
                                    {record.precipitationMm != null
                                        ? `${record.precipitationMm} мм`
                                        : '—'}
                                </p>

                                <p>
                                    Ветер:{' '}
                                    {record.windSpeedKmh != null
                                        ? `${record.windSpeedKmh} км/ч`
                                        : '—'}
                                </p>
                            </article>
                        ))}
                    </div>
                </>
            )}
        </section>
    );
}

export default WeatherHistory;
