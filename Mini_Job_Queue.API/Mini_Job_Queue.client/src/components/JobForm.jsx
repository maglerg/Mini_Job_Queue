import React from 'react';
import { useState } from 'react';
import '../styles/components/JobForm.css';

function JobForm({ onSubmit, isSubmitting }) {
    const [urlsText, setUrlsText] = useState('');

    const handleSubmit = (event) => {
        event.preventDefault();

        const urls = urlsText
            .split('\n')
            .map((url) => url.trim())
            .filter((url) => url.length > 0);

        if (urls.length === 0) {
            return;
        }

        onSubmit(urls);
    };

    return (
        <form className="job-form" onSubmit={handleSubmit}>
            <label htmlFor="urls">
                URL-адреса для проверки
            </label>

            <textarea
                id="urls"
                value={urlsText}
                onChange={(event) => setUrlsText(event.target.value)}
                placeholder={'https://example.com\nhttps://github.com'}
                rows={5}
                disabled={isSubmitting}
            />

            <button type="submit" disabled={isSubmitting}>
                {isSubmitting ? 'Создание задачи...' : 'Проверить URL'}
            </button>
        </form>
    );
}

export default JobForm;
