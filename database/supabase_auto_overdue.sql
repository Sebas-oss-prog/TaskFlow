-- Автоматически переводит просроченные задачи в статус "Просрочено".
-- Отдельная таблица статусов не нужна: используется поле tasks.status.

create extension if not exists pg_cron;

create or replace function public.mark_overdue_tasks()
returns integer
language plpgsql
security definer
as $$
declare
    updated_count integer;
begin
    update public.tasks
    set
        status = 'Просрочено',
        updated_at = now()
    where
        due_date is not null
        and due_date < current_date
        and coalesce(status, '') <> 'Просрочено'
        and coalesce(status, '') <> 'Выполнено';

    get diagnostics updated_count = row_count;
    return updated_count;
end;
$$;

select
    cron.unschedule(jobid)
from cron.job
where jobname = 'mark-overdue-tasks';

select cron.schedule(
    'mark-overdue-tasks',
    '*/10 * * * *',
    $$select public.mark_overdue_tasks();$$
);

-- Для разового запуска сразу после установки:
select public.mark_overdue_tasks();
