import React, { useEffect, useState } from 'react';
import JobForm from '../components/JobForm.jsx';
import { createJob, getJob } from '../api/jobsApi';
import '../styles/App.css';

function HomePage() {
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [job, setJob] = useState(null);
    const [error, setError] = useState('');

    // ID может называться jobId в POST или id в GET.
    const jobId = job?.id ?? job?.jobId;
    const jobStatus = job?.status;

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

    return (
        <main className="app">
            <h1>Mini Job Queue</h1>
            <p>Введите URL-адреса, которые нужно проверить.</p>

            <JobForm
                onSubmit={handleCreateJob}
                isSubmitting={isSubmitting}
            />

            {error && (
                <p className="error-message">{error}</p>
            )}

            {job && (
                <section className="job-info">
                    <h2>Задача</h2>
                    <p>ID: {jobId}</p>
                    <p>Статус: {jobStatus}</p>
                    <p>
                        <strong>Время выполнения:</strong>{' '}
                        {job.executionTimeMs != null
                            ? `${(job.executionTimeMs / 1000).toFixed(2)} сек.`
                            : 'Выполняется...'}
                    </p>
                    {jobStatus !== 'Completed' &&
                        jobStatus !== 'Failed' && (
                            <p>Проверка выполняется...</p>
                        )}

                    {job.results?.length > 0 && (
                        <>
                            <h3>Результаты проверки</h3>

                            {job.results.map((result) => (
                                <article key={result.id}>
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