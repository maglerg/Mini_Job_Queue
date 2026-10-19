
import React, { useState } from 'react';

function AutomationForm({ onSubmit, isSubmitting }) {
    const [urlsText, setUrlsText] = useState('');
    const [intervalSeconds, setIntervalSeconds] = useState(10);

    const handleSubmit = (event) => {
        event.preventDefault();

        const urls = urlsText
            .split(/\r\n|\n|\r/)
            .map(url => url.trim())
            .filter(Boolean);

        if (urls.length === 0) {
            return;
        }

        onSubmit({
            urls,
            intervalSeconds: Number(intervalSeconds)
        });
    };

    return (
        <section className="automation-form-section">
            <h2>Периодическая проверка URL</h2>

            <p>
                Создай правило, которое будет автоматически
                запускать проверку сайтов через заданный интервал.
            </p>

            <form
                className="automation-form"
                onSubmit={handleSubmit}
            >
                <label htmlFor="automation-urls">
                    Список URL
                </label>

                <textarea
                    id="automation-urls"
                    value={urlsText}
                    onChange={(event) =>
                        setUrlsText(event.target.value)
                    }
                    placeholder={
                        'https://example.com\nhttps://google.com'
                    }
                    rows={6}
                    disabled={isSubmitting}
                    required
                />

                <label htmlFor="automation-interval">
                    Интервал проверки
                </label>

                <div className="interval-input">
                    <input
                        id="automation-interval"
                        type="number"
                        min="1"
                        max="86400"
                        value={intervalSeconds}
                        onChange={(event) =>
                            setIntervalSeconds(event.target.value)
                        }
                        disabled={isSubmitting}
                        required
                    />

                    <span>секунд</span>
                </div>

                <p className="input-hint">
                    Например, 10 секунд — новая задача будет
                    планироваться каждые 10 секунд.
                </p>

                <button
                    type="submit"
                    disabled={
                        isSubmitting ||
                        !urlsText.trim() ||
                        Number(intervalSeconds) < 1 ||
                        Number(intervalSeconds) > 86400
                    }
                >
                    {isSubmitting
                        ? 'Создание...'
                        : 'Создать автоматизацию'}
                </button>
            </form>
        </section>
    );
}

export default AutomationForm;
