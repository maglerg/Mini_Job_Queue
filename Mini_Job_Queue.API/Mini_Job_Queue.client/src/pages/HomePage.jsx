
import React, { useEffect, useState } from 'react';

import JobForm from '../components/JobForm.jsx';
import AutomationForm from '../components/AutomationForm.jsx';
import LogAnalysisStatistics from '../components/LogAnalysisStatistics.jsx';

import WeatherCheckForm from '../components/WeatherCheckForm.jsx';
import WeatherCheckResult from '../components/WeatherCheckResult.jsx';

import WeatherForm from '../components/WeatherForm.jsx';
import WeatherTrackingList from '../components/WeatherTrackingList.jsx';
import WeatherHistory from '../components/WeatherHistory.jsx';

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

import {
    checkWeather,
    createWeatherTracking,
    getWeatherTrackings,
    stopWeatherTracking,
    getWeatherHistory
} from '../api/weatherApi';

import '../styles/App.css';
import '../styles/components/Weather.css';

function HomePage() {
    // Текущая вкладка
    const [mode, setMode] = useState('url');

    // Анализ логов
    const [logs, setLogs] = useState('');

    // Общие состояния задач и автоматизации
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [job, setJob] = useState(null);
    const [automation, setAutomation] = useState(null);
    const [error, setError] = useState('');

    const [automationRuns, setAutomationRuns] = useState([]);
    const [isStoppingAutomation, setIsStoppingAutomation] = useState(false);

    // Разовая проверка погоды
    const [weatherCheckResult, setWeatherCheckResult] = useState(null);
    const [isWeatherChecking, setIsWeatherChecking] = useState(false);

    // Состояния автоматизации погоды
    const [weatherTrackings, setWeatherTrackings] = useState([]);
    const [selectedWeatherId, setSelectedWeatherId] = useState(null);
    const [weatherRecords, setWeatherRecords] = useState([]);

    const [isWeatherSubmitting, setIsWeatherSubmitting] = useState(false);
    const [isWeatherLoading, setIsWeatherLoading] = useState(false);
    const [stoppingWeatherId, setStoppingWeatherId] = useState(null);

    // Идентификатор и статус текущей задачи
    const jobId = job?.id ?? job?.jobId;
    const jobStatus = job?.status;

    const isLogJob =
        job?.type === 'LogAnalysis' || mode === 'logs';

    // ========================================
    // Разовая проверка URL
    // ========================================

    const handleCreateJob = async (urls) => {
        try {
            setIsSubmitting(true);
            setError('');
            setJob(null);
            setAutomation(null);
            setAutomationRuns([]);

            const createdJob = await createJob(urls);
            setJob(createdJob);
        } catch (err) {
            setError(err.message);
        } finally {
            setIsSubmitting(false);
        }
    };

    // ========================================
    // Анализ логов
    // ========================================

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
            setAutomationRuns([]);

            const createdJob = await createLogAnalysisJob(logs);
            setJob(createdJob);
        } catch (err) {
            setError(err.message);
        } finally {
            setIsSubmitting(false);
        }
    };

    // ========================================
    // Создание автоматизации URL
    // ========================================

    const handleCreateAutomation = async ({
        urls,
        intervalSeconds
    }) => {
        try {
            setIsSubmitting(true);
            setError('');
            setJob(null);
            setAutomation(null);
            setAutomationRuns([]);

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

    // ========================================
    // Остановка автоматизации URL
    // ========================================

    const handleStopAutomation = async () => {
        if (!automation?.ruleId || isStoppingAutomation) {
            return;
        }

        try {
            setIsStoppingAutomation(true);
            setError('');

            const result = await stopAutomationRule(
                automation.ruleId
            );

            setAutomation((previous) => ({
                ...previous,
                isEnabled: result?.isEnabled ?? false,
                nextRunAt: null
            }));
        } catch (err) {
            setError(err.message);
        } finally {
            setIsStoppingAutomation(false);
        }
    };

    // ========================================
    // Разовая проверка погоды
    // ========================================

    const handleWeatherCheck = async (locationName) => {
        try {
            setIsWeatherChecking(true);
            setError('');
            setWeatherCheckResult(null);

            const result = await checkWeather(locationName);

            setWeatherCheckResult(result);
        } catch (err) {
            setError(err.message);
        } finally {
            setIsWeatherChecking(false);
        }
    };

    // ========================================
    // Создание отслеживания погоды
    // ========================================

    const handleCreateWeatherTracking = async ({
        locationName,
        intervalMinutes
    }) => {
        try {
            setIsWeatherSubmitting(true);
            setError('');

            const createdTracking = await createWeatherTracking({
                locationName,
                intervalMinutes
            });

            setWeatherTrackings((previous) => [
                createdTracking,
                ...previous.filter(
                    (item) => item.id !== createdTracking.id
                )
            ]);

            setSelectedWeatherId(createdTracking.id);
            setWeatherRecords([]);

            return true;
        } catch (err) {
            setError(err.message);
            return false;
        } finally {
            setIsWeatherSubmitting(false);
        }
    };

    // ========================================
    // Остановка отслеживания погоды
    // ========================================

    const handleStopWeatherTracking = async (id) => {
        if (stoppingWeatherId !== null) {
            return;
        }

        try {
            setStoppingWeatherId(id);
            setError('');

            await stopWeatherTracking(id);

            setWeatherTrackings((previous) =>
                previous.map((tracking) =>
                    tracking.id === id
                        ? {
                            ...tracking,
                            isEnabled: false,
                            nextRunAt: null
                        }
                        : tracking
                )
            );
        } catch (err) {
            setError(err.message);
        } finally {
            setStoppingWeatherId(null);
        }
    };

    // ========================================
    // Обновление списка отслеживаний погоды
    // ========================================

    useEffect(() => {
        if (mode !== 'weather') {
            return;
        }

        let cancelled = false;
        let timeoutId;

        const loadTrackings = async () => {
            try {
                const data = await getWeatherTrackings();

                if (cancelled) {
                    return;
                }

                setWeatherTrackings(data);

                if (
                    selectedWeatherId &&
                    !data.some(
                        (item) => item.id === selectedWeatherId
                    )
                ) {
                    setSelectedWeatherId(null);
                    setWeatherRecords([]);
                }
            } catch (err) {
                if (!cancelled) {
                    setError(err.message);
                }
            }

            if (!cancelled) {
                timeoutId = setTimeout(loadTrackings, 5000);
            }
        };

        loadTrackings();

        return () => {
            cancelled = true;
            clearTimeout(timeoutId);
        };
    }, [mode, selectedWeatherId]);

    // ========================================
    // Обновление истории погоды
    // ========================================

    useEffect(() => {
        if (mode !== 'weather' || !selectedWeatherId) {
            setWeatherRecords([]);
            setIsWeatherLoading(false);
            return;
        }

        let cancelled = false;
        let timeoutId;

        const loadHistory = async () => {
            setIsWeatherLoading(true);

            try {
                const data = await getWeatherHistory(
                    selectedWeatherId
                );

                if (!cancelled) {
                    setWeatherRecords(data);
                }
            } catch (err) {
                if (!cancelled) {
                    setError(err.message);
                }
            } finally {
                if (!cancelled) {
                    setIsWeatherLoading(false);
                }
            }

            if (!cancelled) {
                timeoutId = setTimeout(loadHistory, 5000);
            }
        };

        loadHistory();

        return () => {
            cancelled = true;
            clearTimeout(timeoutId);
        };
    }, [mode, selectedWeatherId]);

    // ========================================
    // Поллинг статуса разовой задачи
    // ========================================

    useEffect(() => {
        if (!jobId) {
            return;
        }

        if (
            jobStatus === 'Completed' ||
            jobStatus === 'Failed'
        ) {
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

    // ========================================
    // Поллинг истории запусков автоматизации URL
    // ========================================

    useEffect(() => {
        if (!automation?.ruleId || !automation?.isEnabled) {
            return;
        }

        let cancelled = false;
        let timeoutId;

        const pollRuns = async () => {
            try {
                const runs = await getAutomationRuns(
                    automation.ruleId
                );

                if (cancelled) {
                    return;
                }

                setAutomationRuns(runs);
            } catch (err) {
                if (!cancelled) {
                    setError(err.message);
                }
            }

            if (!cancelled) {
                timeoutId = setTimeout(pollRuns, 2000);
            }
        };

        pollRuns();

        return () => {
            cancelled = true;
            clearTimeout(timeoutId);
        };
    }, [automation?.ruleId, automation?.isEnabled]);

    // ========================================
    // Переключение вкладок
    // ========================================

    const changeMode = (nextMode) => {
        setMode(nextMode);
        setJob(null);
        setAutomation(null);
        setAutomationRuns([]);
        setWeatherCheckResult(null);
        setError('');
    };

    // Выбранное отслеживание для отображения истории
    const selectedWeatherTracking = weatherTrackings.find(
        (tracking) => tracking.id === selectedWeatherId
    );

    // ========================================
    // Интерфейс
    // ========================================

    return (
        <main className="app">
            <h1>Mini Automation Engine</h1>

            <p>
                Создавай фоновые задачи и настраивай
                автоматическую обработку.
            </p>

            {/* Меню вкладок */}
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
                    Автоматизация URL
                </button>

                <button
                    type="button"
                    className={mode === 'weather-check' ? 'active' : ''}
                    onClick={() => changeMode('weather-check')}
                >
                    Погода
                </button>

                <button
                    type="button"
                    className={mode === 'weather' ? 'active' : ''}
                    onClick={() => changeMode('weather')}
                >
                    Автоматизация погоды
                </button>
            </nav>

            {/* ========================================
                Разовая проверка URL
            ======================================== */}

            {mode === 'url' && (
                <>
                    <h2>Разовая проверка сайтов</h2>

                    <JobForm
                        onSubmit={handleCreateJob}
                        isSubmitting={isSubmitting}
                    />
                </>
            )}

            {/* ========================================
                Анализ Linux-логов
            ======================================== */}

            {mode === 'logs' && (
                <section className="log-form-section">
                    <h2>Анализ Linux-логов</h2>

                    <p>
                        Вставьте текст логов. Сервер обработает его
                        в фоновом режиме и подготовит статистику.
                    </p>

                    <p>
                        Примеры логов можно взять из открытого набора
                        данных{' '}
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
                            placeholder="Jun 14 15:16:01 combo sshd[19939]: authentication failure; rhost=218.188.2.4"
                            rows={12}
                            maxLength={500000}
                            disabled={isSubmitting}
                        />

                        <p className="input-hint">
                            Строк:{' '}
                            {logs
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

            {/* ========================================
                Автоматизация URL
            ======================================== */}

            {mode === 'automation' && (
                <AutomationForm
                    onSubmit={handleCreateAutomation}
                    isSubmitting={isSubmitting}
                />
            )}

            {/* Информация об автоматизации URL */}
            {mode === 'automation' && automation && (
                <>
                    <section className="job-info">
                        <h2>Автоматизация создана</h2>

                        <p>
                            <strong>ID правила:</strong>{' '}
                            {automation.ruleId}
                        </p>

                        <p>
                            <strong>Тип:</strong>{' '}
                            {automation.type === 'UrlCheck'
                                ? 'Периодическая проверка URL'
                                : automation.type}
                        </p>

                        <p>
                            <strong>Интервал:</strong>{' '}
                            {automation.intervalSeconds} сек.
                        </p>

                        <p>
                            <strong>Статус:</strong>{' '}
                            {automation.isEnabled
                                ? 'Включена'
                                : 'Отключена'}
                        </p>

                        <p>
                            <strong>Следующий запуск:</strong>{' '}
                            {automation.nextRunAt
                                ? new Date(
                                    automation.nextRunAt
                                ).toLocaleString()
                                : '—'}
                        </p>

                        <p>
                            Правило сохранено. Scheduler создаст
                            задачу, когда наступит время запуска.
                        </p>

                        {automation.isEnabled && (
                            <button
                                type="button"
                                className="stop-automation-button"
                                onClick={handleStopAutomation}
                                disabled={isStoppingAutomation}
                            >
                                {isStoppingAutomation
                                    ? 'Остановка...'
                                    : 'Остановить автоматизацию'}
                            </button>
                        )}
                    </section>

                    <section className="job-info automation-history">
                        <h2>История запусков</h2>

                        {automationRuns.length === 0 ? (
                            <p>
                                Запусков пока нет. Первый запуск
                                будет выполнен по расписанию.
                            </p>
                        ) : (
                            automationRuns.map((run, index) => (
                                <article key={run.id}>
                                    <h3>
                                        Запуск №
                                        {automationRuns.length - index}
                                    </h3>

                                    <p>
                                        <strong>ID задачи:</strong>{' '}
                                        <span className="job-id">
                                            {run.id}
                                        </span>
                                    </p>

                                    <p>
                                        <strong>Создана:</strong>{' '}
                                        {run.createdAt
                                            ? new Date(
                                                run.createdAt
                                            ).toLocaleString()
                                            : '—'}
                                    </p>

                                    <p>
                                        <strong>Статус:</strong>{' '}
                                        {run.status ?? '—'}
                                    </p>

                                    <p>
                                        <strong>Время выполнения:</strong>{' '}
                                        {run.executionTimeMs != null
                                            ? `${run.executionTimeMs} мс`
                                            : '—'}
                                    </p>

                                    {run.results?.length > 0 && (
                                        <>
                                            <h4>Результаты проверки URL</h4>

                                            {run.results.map((result) => (
                                                <div
                                                    key={result.id ?? result.url}
                                                    style={{
                                                        marginBottom: '10px',
                                                        padding: '10px',
                                                        background: '#f8f9fa',
                                                        borderRadius: '8px'
                                                    }}
                                                >
                                                    <p>
                                                        <strong>URL:</strong>{' '}
                                                        {result.url}
                                                    </p>

                                                    <p>
                                                        <strong>HTTP-статус:</strong>{' '}
                                                        {result.httpStatusCode ?? '—'}
                                                    </p>

                                                    <p>
                                                        <strong>Время ответа:</strong>{' '}
                                                        {result.responseTimeMs != null
                                                            ? `${result.responseTimeMs} мс`
                                                            : '—'}
                                                    </p>

                                                    {result.error && (
                                                        <p className="error-message">
                                                            Ошибка: {result.error}
                                                        </p>
                                                    )}
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

            {/* ========================================
                Разовая проверка погоды
            ======================================== */}

            {mode === 'weather-check' && (
                <section className="weather-section">
                    <h2>Разовая проверка погоды</h2>

                    <p className="weather-intro">
                        Укажи город, чтобы получить актуальные
                        погодные данные без создания автоматизации.
                    </p>

                    <WeatherCheckForm
                        onSubmit={handleWeatherCheck}
                        isSubmitting={isWeatherChecking}
                    />

                    {weatherCheckResult && (
                        <WeatherCheckResult
                            result={weatherCheckResult}
                        />
                    )}
                </section>
            )}

            {/* ========================================
                Автоматизация погоды
            ======================================== */}

            {mode === 'weather' && (
                <section className="weather-section">
                    <div>
                        <h2>Автоматизация погоды</h2>

                        <p className="weather-intro">
                            Настраивай периодический сбор погодных
                            данных, отслеживай состояние задач и
                            просматривай историю измерений по каждому
                            городу.
                        </p>
                    </div>

                    <WeatherForm
                        onSubmit={handleCreateWeatherTracking}
                        isSubmitting={isWeatherSubmitting}
                    />

                    <WeatherTrackingList
                        trackings={weatherTrackings}
                        selectedId={selectedWeatherId}
                        onSelect={setSelectedWeatherId}
                        onStop={handleStopWeatherTracking}
                        isStopping={stoppingWeatherId !== null}
                    />

                    <WeatherHistory
                        tracking={selectedWeatherTracking}
                        records={weatherRecords}
                        isLoading={isWeatherLoading}
                    />
                </section>
            )}

            {/* ========================================
                Общая ошибка
            ======================================== */}

            {error && (
                <p className="error-message" role="alert">
                    {error}
                </p>
            )}

            {/* ========================================
                Результат разовой задачи URL / логов
            ======================================== */}

            {job && (mode === 'url' || mode === 'logs') && (
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
