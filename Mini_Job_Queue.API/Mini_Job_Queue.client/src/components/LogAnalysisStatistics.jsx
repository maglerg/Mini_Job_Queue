
import React from 'react';

function LogAnalysisStatistics({ result }) {
    if (!result) {
        return null;
    }

    const categoryCounts = result.categoryCounts ?? {};
    const topMessages = result.topMessages ?? {};
    const failedHosts = result.failedAuthenticationByHost ?? {};

    const formatCategory = (category) => {
        const names = {
            AuthenticationFailure: 'Ошибки аутентификации',
            UserSession: 'Пользовательские сессии',
            NetworkConnection: 'Сетевые подключения',
            SystemError: 'Системные ошибки',
            Other: 'Прочие события'
        };

        return names[category] ?? category;
    };

    return (
        <section className="job-info log-statistics">
            <h2>Статистика анализа логов</h2>

            <div className="statistics-grid">
                <article>
                    <span>Всего строк</span>
                    <strong>{result.totalLines}</strong>
                </article>

                <article>
                    <span>Распознано</span>
                    <strong>{result.parsedLines}</strong>
                </article>

                <article>
                    <span>Некорректных</span>
                    <strong>{result.invalidLines}</strong>
                </article>
            </div>

            <h3>Категории событий</h3>

            {Object.keys(categoryCounts).length > 0 ? (
                <div className="statistics-list">
                    {Object.entries(categoryCounts)
                        .sort((a, b) => b[1] - a[1])
                        .map(([category, count]) => (
                            <article key={category}>
                                <span>{formatCategory(category)}</span>
                                <strong>{count}</strong>
                            </article>
                        ))}
                </div>
            ) : (
                <p>Категории событий не обнаружены.</p>
            )}

            <h3>Частые сообщения</h3>

            {Object.keys(topMessages).length > 0 ? (
                <div className="statistics-list">
                    {Object.entries(topMessages)
                        .sort((a, b) => b[1] - a[1])
                        .map(([message, count]) => (
                            <article key={message}>
                                <p className="log-message">{message}</p>
                                <strong>Повторений: {count}</strong>
                            </article>
                        ))}
                </div>
            ) : (
                <p>Сообщений нет.</p>
            )}

            <h3>Ошибки аутентификации по IP</h3>

            {Object.keys(failedHosts).length > 0 ? (
                <div className="statistics-list">
                    {Object.entries(failedHosts)
                        .sort((a, b) => b[1] - a[1])
                        .map(([host, count]) => (
                            <article key={host}>
                                <span>{host}</span>
                                <strong>{count}</strong>
                            </article>
                        ))}
                </div>
            ) : (
                <p>Неудачных попыток с определённым IP не обнаружено.</p>
            )}
        </section>
    );
}

export default LogAnalysisStatistics;
