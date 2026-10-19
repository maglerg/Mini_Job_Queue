import React, { useEffect, useState } from 'react';
import JobForm from '../components/JobForm.jsx';
import AutomationForm from '../components/AutomationForm.jsx';
import LogAnalysisStatistics from '../components/LogAnalysisStatistics.jsx';

// Импорт API (проверь, чтобы пути были точно такими)
import {
    createAutomationRule,
    stopAutomationRule,
    getAutomationRuns
} from '../api/automationApi';

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
    const [automation, setAutomation] = useState(null);
    const [error, setError] = useState('');

    const jobId = job?.id ?? job?.jobId;
    const jobStatus = job?.status;

    // --- Стейты для истории запусков автоматизации ---
    const [automationRuns, setAutomationRuns] = useState([]);
    const [isStoppingAutomation, setIsStoppingAutomation] = useState(false);

    const isLogJob = job?.type === 'LogAnalysis' || mode === 'logs';

    const handleCreateJob = async (urls) => {
        try {
            setIsSubmitting(true);
            setError('');
            setJob(null);
            setAutomation(null);

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
            setError('Вставьте текст логов перед отправкой.');
            return;
        }

        try {
            setIsSubmitting(true);
            setError('');
            setJob(null);
            setAutomation(null);

            const createdJob = await createLogAnalysisJob(logs);
            setJob(createdJob);
        } catch (err) {
            setError(err.message);
        } finally {
            setIsSubmitting(false);
        }
    };

    const handleCreateAutomation = async ({ urls, intervalSeconds }) => {
        try {
            setIsSubmitting(true);
            setError('');
            setJob(null);
            setAutomation(null);
            setAutomationRuns([]); // Очищаем старую историю при создании новой

            const createdRule = await createAutomationRule({
                urls,
                intervalSeconds
            });

            setAutomation(createdRule);
        } catch (err) {
            setError(err.message);
        } finally {
            setIsSubmitting(false);
        }
    };

    const handleStopAutomation = async () => {
        if (!automation?.ruleId || isStoppingAutomation) {
            return;
        }

        try {
            setIsStoppingAutomation(true);
            setError('');

            const result = await stopAutomationRule(automation.ruleId);
            console.log("Ответ сервера на остановку:", result);
            setAutomation((previous) => ({
                ...previous,
                isEnabled: result.isEnabled,
                nextRunAt: null
            }));
        } catch (err) {
            setError(err.message);
        } finally {
            setIsStoppingAutomation(false);
        }
    };

    // --- Поллинг статуса задачи ---
    useEffect(() => {
        if (!jobId) return;

        if (jobStatus === 'Completed' || jobStatus === 'Failed') {
            return;
        }

        let cancelled = false;
        let timeoutId;

        const pollJob = async () => {
            try {
                const updatedJob = await getJob(jobId);
                if (cancelled) return;

                setJob(updatedJob);

                if (updatedJob.status !== 'Completed' && updatedJob.status !== 'Failed') {
                    timeoutId = setTimeout(pollJob, 1000);
                }
            } catch (err) {
                if (!cancelled) setError(err.message);
            }
        };

        timeoutId = setTimeout(pollJob, 1000);

        return () => {
            cancelled = true;
            clearTimeout(timeoutId);
        };
    }, [jobId, jobStatus]);

    // --- Поллинг истории запусков автоматизации ---и
    useEffect(() => {
        // Если правило отключено или его нет — даже не начинаем
        if (!automation?.ruleId || !automation?.isEnabled) {
            return;
        }

        let cancelled = false;
        let timeoutId;

        const pollRuns = async () => {
            // Внутри функции тоже проверяем, не отключили ли его во время запроса
            if (!automation?.isEnabled) return;

            try {
                const runs = await getAutomationRuns(automation.ruleId);
                if (cancelled) return;
                setAutomationRuns(runs);
            } catch (err) {
                if (!cancelled) setError(err.message);
            }

            if (!cancelled && automation?.isEnabled) {
                timeoutId = setTimeout(pollRuns, 2000);
            }
        };

        pollRuns();

        return () => {
            cancelled = true;
            clearTimeout(timeoutId);
        };
    }, [automation?.ruleId, automation?.isEnabled]);

    const changeMode = (nextMode) => {
        setMode(nextMode);
        setJob(null);
        setAutomation(null); // Сбрасываем автоматизацию при смене вкладки
        setAutomationRuns([]); // Сбрасываем историю
        setError('');
    };

    return (
        <main className="app">
            <h1>Mini Automation Engine</h1>
            <p>Создавай фоновые задачи и настраивай автоматическую обработку.</p>

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
                <button
                    type="button"
                    className={mode === 'automation' ? 'active' : ''}
                    onClick={() => changeMode('automation')}
                >
                    Автоматизация
                </button>
            </nav>

            {/* --- РАЗОВАЯ ПРОВЕРКА URL --- */}
            {mode === 'url' && (
                <>
                    <h2>Разовая проверка сайтов</h2>
                    <JobForm onSubmit={handleCreateJob} isSubmitting={isSubmitting} />
                </>
            )}

            {/* --- АНАЛИЗ ЛОГОВ --- */}
            {mode === 'logs' && (
                <section className="log-form-section">
                    <h2>Анализ Linux-логов</h2>
                    <p>Вставьте текст логов. Сервер обработает его в фоновом режиме и подготовит статистику.</p>
                    <p>
                        Примеры логов можно взять из открытого набора данных{' '}
                        <a href="https://github.com/logpai/loghub/blob/master/Linux/Linux_2k.log#L1" target="_blank" rel="noopener noreferrer">
                            Linux_2k.log на GitHub
                        </a>.
                    </p>

                    <form className="log-form" onSubmit={handleCreateLogAnalysis}>
                        <label htmlFor="log-input">Текст логов</label>
                        <textarea
                            id="log-input"
                            value={logs}
                            onChange={(event) => setLogs(event.target.value)}
                            placeholder="Jun 14 15:16:01 combo sshd[19939]: authentication failure; rhost=218.188.2.4"
                            rows={12}
                            maxLength={500000}
                            disabled={isSubmitting}
                        />
                        <p className="input-hint">
                            Строк: {logs ? logs.split(/\r\n|\n|\r/).length : 0}
                        </p>
                        <button type="submit" disabled={isSubmitting || !logs.trim()}>
                            {isSubmitting ? 'Отправка...' : 'Анализировать логи'}
                        </button>
                    </form>
                </section>
            )}

            {/* --- АВТОМАТИЗАЦИЯ --- */}
            {mode === 'automation' && (
                <>
                    <AutomationForm onSubmit={handleCreateAutomation} isSubmitting={isSubmitting} />
                    <section className="automation-info">
                        <h3>Как это работает</h3>
                        <p>Scheduler проверяет расписание правил, создаёт задачи и помещает их в очередь. JobWorker выполняет проверки в фоне.</p>
                    </section>
                </>
            )}

            {/* --- БЛОК ОШИБКИ --- */}
            {error && <p className="error-message">{error}</p>}

            {/* --- ОТОБРАЖЕНИЕ АВТОМАТИЗАЦИИ И ЕЁ ИСТОРИИ --- */}
            {automation && (
                <>
                    <section className="job-info">
                        <h2>Автоматизация создана</h2>
                        <p><strong>ID правила:</strong> {automation.ruleId}</p>
                        <p><strong>Тип:</strong> {automation.type === 'UrlCheck' ? 'Периодическая проверка URL' : automation.type}</p>
                        <p><strong>Интервал:</strong> {automation.intervalSeconds} сек.</p>
                        <p><strong>Статус:</strong> {automation.isEnabled ? 'Включена' : 'Отключена'}</p>
                        <p><strong>Следующий запуск:</strong> {automation.nextRunAt ? new Date(automation.nextRunAt).toLocaleString() : '—'}</p>
                        <p>Правило сохранено. Scheduler создаст задачу, когда наступит время запуска.</p>

                        {automation.isEnabled && (
                            <button
                                type="button"
                                className="stop-automation-button"
                                onClick={handleStopAutomation}
                                disabled={isStoppingAutomation}
                            >
                                {isStoppingAutomation ? 'Остановка...' : 'Остановить автоматизацию'}
                            </button>
                        )}
                    </section>

                    <section className="job-info automation-history">
                        <h2>История запусков</h2>
                        {automationRuns.length === 0 ? (
                            <p>Запусков пока нет. Первый запуск будет выполнен по расписанию.</p>
                        ) : (
                            automationRuns.map((run, index) => (
                                <article key={run.id}>
                                    <h3>Запуск №{automationRuns.length - index}</h3>
                                    <p><strong>ID задачи:</strong> <span className="job-id">{run.id}</span></p>
                                    <p><strong>Создана:</strong> {run.createdAt ? new Date(run.createdAt).toLocaleString() : '—'}</p>
                                    <p><strong>Статус:</strong> {run.status ?? '—'}</p>
                                    <p><strong>Время выполнения:</strong> {run.executionTimeMs != null ? `${run.executionTimeMs} мс` : '—'}</p>

                                    {run.results?.length > 0 && (
                                        <>
                                            <h4>Результаты проверки URL</h4>
                                            {run.results.map((result) => (
                                                <div key={result.id ?? result.url} style={{ marginBottom: '10px', padding: '10px', background: '#f8f9fa', borderRadius: '8px' }}>
                                                    <p><strong>URL:</strong> {result.url}</p>
                                                    <p><strong>HTTP-статус:</strong> {result.httpStatusCode ?? '—'}</p>
                                                    <p><strong>Время ответа:</strong> {result.responseTimeMs != null ? `${result.responseTimeMs} мс` : '—'}</p>
                                                    {result.error && <p className="error-message">Ошибка: {result.error}</p>}
                                                </div>
                                            ))}
                                        </>
                                    )}
                                </article>
                            ))
                        )}
                    </section>
                </>
            )}

            {/* --- ОТОБРАЖЕНИЕ РАЗОВОЙ ЗАДАЧИ (JOB) --- */}
            {job && (
                <section className="job-info">
                    <h2>Задача</h2>
                    <p className="job-id"><strong>ID:</strong> {jobId}</p>
                    <p>
                        <strong>Тип:</strong>{' '}
                        {job.type === 'LogAnalysis' ? 'Анализ логов' : job.type === 'UrlCheck' ? 'Проверка URL' : job.type ?? '—'}
                    </p>
                    <p><strong>Статус:</strong> {jobStatus}</p>
                    <p>
                        <strong>Время выполнения:</strong>{' '}
                        {job.executionTimeMs != null ? `${(job.executionTimeMs / 1000).toFixed(2)} сек.` : '—'}
                    </p>

                    {jobStatus !== 'Completed' && jobStatus !== 'Failed' && (
                        <p>Задача обрабатывается в фоне...</p>
                    )}

                    {jobStatus === 'Failed' && (
                        <p className="error-message">Обработка задачи завершилась ошибкой.</p>
                    )}

                    {jobStatus === 'Completed' && isLogJob && (
                        <LogAnalysisStatistics result={job.logAnalysisResult} />
                    )}

                    {jobStatus === 'Completed' && !isLogJob && job.results?.length > 0 && (
                        <>
                            <h3>Результаты проверки URL</h3>
                            {job.results.map((result) => (
                                <article key={result.id ?? result.url}>
                                    <p>URL: {result.url}</p>
                                    <p>HTTP-статус: {result.httpStatusCode ?? '—'}</p>
                                    <p>Время ответа: {result.responseTimeMs ?? '—'} мс</p>
                                    {result.error && (
                                        <p className="error-message">Ошибка: {result.error}</p>
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