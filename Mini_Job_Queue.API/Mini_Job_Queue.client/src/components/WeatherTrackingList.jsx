
import React from 'react';

function formatDate(value) {
    return value ? new Date(value).toLocaleString() : '—';
}

function WeatherTrackingList({
    trackings,
    selectedId,
    onSelect,
    onStop,
    isStopping
}) {
    if (trackings.length === 0) {
        return (
            <section className="weather-panel">
                <h2>Отслеживаемые города</h2>
                <p>
                    Пока нет отслеживаний. Создай первое с помощью формы.
                </p>
            </section>
        );
    }

    return (
        <section className="weather-panel">
            <h2>Отслеживаемые города</h2>

            <div className="weather-tracking-list">
                {trackings.map((tracking) => {
                    const id = tracking.id;
                    const isSelected = selectedId === id;

                    return (
                        <article
                            className={`weather-tracking-card ${isSelected ? 'selected' : ''
                                }`}
                            key={id}
                        >
                            <div className="weather-tracking-heading">
                                <h3>{tracking.locationName}</h3>

                                <span
                                    className={`weather-status ${tracking.isEnabled
                                            ? 'enabled'
                                            : 'disabled'
                                        }`}
                                >
                                    {tracking.isEnabled
                                        ? 'Активно'
                                        : 'Остановлено'}
                                </span>
                            </div>

                            <p>
                                <strong>Интервал:</strong>{' '}
                                {tracking.intervalMinutes} мин.
                            </p>

                            <p>
                                <strong>Создано:</strong>{' '}
                                {formatDate(tracking.createdAt)}
                            </p>

                            <p>
                                <strong>Последний запуск:</strong>{' '}
                                {formatDate(tracking.lastRunAt)}
                            </p>

                            <p>
                                <strong>Следующий запуск:</strong>{' '}
                                {tracking.isEnabled
                                    ? formatDate(tracking.nextRunAt)
                                    : '—'}
                            </p>

                            <div className="weather-card-actions">
                                <button
                                    type="button"
                                    className="weather-secondary-button"
                                    onClick={() => onSelect(id)}
                                >
                                    {isSelected
                                        ? 'История выбрана'
                                        : 'История погоды'}
                                </button>

                                {tracking.isEnabled && (
                                    <button
                                        type="button"
                                        className="weather-stop-button"
                                        onClick={() => onStop(id)}
                                        disabled={isStopping}
                                    >
                                        {isStopping
                                            ? 'Остановка...'
                                            : 'Остановить'}
                                    </button>
                                )}
                            </div>
                        </article>
                    );
                })}
            </div>
        </section>
    );
}

export default WeatherTrackingList;
