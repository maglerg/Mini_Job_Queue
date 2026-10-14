
import React, { useEffect, useState } from 'react';
import JobForm from '../components/JobForm.jsx';
import LogAnalysisStatistics from '../components/LogAnalysisStatistics.jsx';
import {
    createJob,
    createLogAnalysisJob,
    getJob
} from '../api/jobsApi';
import '../styles/App.css';

function HomePage() {
    const [mode, setMode] = useState('url');
    const [logs, setLogs] = useState('');

    const [isSubmitting, setIsSubmitting] = useState(false);
    const [job, setJob] = useState(null);
    const [error, setError] = useState('');

    const jobId = job?.id ?? job?.jobId;
    const jobStatus = job?.status;

    const isLogJob =
        job?.type === 'LogAnalysis' || mode === 'logs';

    const handleCreateJob = async (urls) => {
        try {
            setIsSubmitting(true);
            setError('');
            setJob(null);

            const createdJob = await createJob(urls);
            setJob(createdJob);
        } catch (err) {
            setError(err.message);
        } finally {
            setIsSubmitting(false);
        }
    };

    const handleCreateLogAnalysis = async (event) => {
        event.preventDefault();

        if (!logs.trim()) {
            setError('Вставь текст логов перед отправкой.');
            return;
        }

        try {
            setIsSubmitting(true);
            setError('');
            setJob(null);

            const createdJob = await createLogAnalysisJob(logs);
            setJob(createdJob);
        } catch (err) {
            setError(err.message);
        } finally {
            setIsSubmitting(false);
        }
    };

    useEffect(() => {
        if (!jobId) {
            return;
        }

        if (jobStatus === 'Completed' || jobStatus === 'Failed') {
            return;
        }

        let cancelled = false;
        let timeoutId;

        const pollJob = async () => {
            try {
                const updatedJob = await getJob(jobId);

                if (cancelled) {
                    return;
                }

                setJob(updatedJob);

                if (
                    updatedJob.status !== 'Completed' &&
                    updatedJob.status !== 'Failed'
                ) {
                    timeoutId = setTimeout(pollJob, 1000);
                }
            } catch (err) {
                if (!cancelled) {
                    setError(err.message);
                }
            }
        };

        timeoutId = setTimeout(pollJob, 1000);

        return () => {
            cancelled = true;
            clearTimeout(timeoutId);
        };
    }, [jobId, jobStatus]);

    const changeMode = (nextMode) => {
        setMode(nextMode);
        setJob(null);
        setError('');
    };

    return (
        <main className="app">
            <h1>Mini Job Queue</h1>
            <p>
                Выбери тип фоновой задачи, которую нужно выполнить.
            </p>

            <nav className="mode-menu" aria-label="Тип задачи">
                <button
                    type="button"
                    className={mode === 'url' ? 'active' : ''}
                    onClick={() => changeMode('url')}
                >
                    Проверка URL
                </button>

                <button
                    type="button"
                    className={mode === 'logs' ? 'active' : ''}
                    onClick={() => changeMode('logs')}
                >
                    Анализ логов
                </button>
            </nav>

            {mode === 'url' ? (
                <>
                    <h2>Проверка доступности сайтов</h2>

                    <JobForm
                        onSubmit={handleCreateJob}
                        isSubmitting={isSubmitting}
                    />
                </>
            ) : (
                <section className="log-form-section">
                    <h2>Анализ Linux-логов</h2>
                    <p>
                        Вставьte текст логов. Сервер обработает его
                        в фоновом режиме и подготовит статистику.
                    </p>
                        <p>
                            Примеры логов можно взять из открытого набора данных{' '}
                            <a
                                href="https://github.com/logpai/loghub/blob/master/Linux/Linux_2k.log#L1"
                                target="_blank"
                                rel="noopener noreferrer"
                            >
                                Linux_2k.log на GitHub
                            </a>.
                        </p>

                    <form
                        className="log-form"
                        onSubmit={handleCreateLogAnalysis}
                    >
                        <label htmlFor="log-input">
                            Текст логов
                        </label>

                        <textarea
                            id="log-input"
                            value={logs}
                            onChange={(event) =>
                                setLogs(event.target.value)
                            }
                            placeholder={
                                'Jun 14 15:16:01 combo sshd[19939]: authentication failure; rhost=218.188.2.4'
                            }
                            rows={12}
                            maxLength={500000}
                            disabled={isSubmitting}
                        />

                        <p className="input-hint">
                            Строк: {logs
                                ? logs.split(/\r\n|\n|\r/).length
                                : 0}
                        </p>

                        <button
                            type="submit"
                            disabled={isSubmitting || !logs.trim()}
                        >
                            {isSubmitting
                                ? 'Отправка...'
                                : 'Анализировать логи'}
                        </button>
                    </form>
                </section>
            )}

            {error && (
                <p className="error-message">{error}</p>
            )}

            {job && (
                <section className="job-info">
                    <h2>Задача</h2>

                    <p className="job-id">
                        <strong>ID:</strong> {jobId}
                    </p>

                    <p>
                        <strong>Тип:</strong>{' '}
                        {job.type === 'LogAnalysis'
                            ? 'Анализ логов'
                            : job.type === 'UrlCheck'
                                ? 'Проверка URL'
                                : job.type ?? '—'}
                    </p>

                    <p>
                        <strong>Статус:</strong> {jobStatus}
                    </p>

                    <p>
                        <strong>Время выполнения:</strong>{' '}
                        {job.executionTimeMs != null
                            ? `${(job.executionTimeMs / 1000).toFixed(2)} сек.`
                            : '—'}
                    </p>

                    {jobStatus !== 'Completed' &&
                        jobStatus !== 'Failed' && (
                            <p>Задача обрабатывается в фоне...</p>
                        )}

                    {jobStatus === 'Failed' && (
                        <p className="error-message">
                            Обработка задачи завершилась ошибкой.
                        </p>
                    )}

                    {jobStatus === 'Completed' && isLogJob && (
                        <LogAnalysisStatistics
                            result={job.logAnalysisResult}
                        />
                    )}

                    {jobStatus === 'Completed' &&
                        !isLogJob &&
                        job.results?.length > 0 && (
                            <>
                                <h3>Результаты проверки URL</h3>

                                {job.results.map((result) => (
                                    <article key={result.id ?? result.url}>
                                        <p>URL: {result.url}</p>
                                        <p>
                                            HTTP-статус:{' '}
                                            {result.httpStatusCode ?? '—'}
                                        </p>
                                        <p>
                                            Время ответа:{' '}
                                            {result.responseTimeMs ?? '—'} мс
                                        </p>

                                        {result.error && (
                                            <p className="error-message">
                                                Ошибка: {result.error}
                                            </p>
                                        )}
                                    </article>
                                ))}
                            </>
                        )}
                </section>
            )}
        </main>
    );
}

export default HomePage;
