-- Additive migration: record the selected site on appointments.
-- Existing appointments are kept; the application requires a site for new appointments.
ALTER TABLE public.cita
    ADD COLUMN IF NOT EXISTS id_sede integer;

DO $$
BEGIN
    IF to_regclass('public.sede') IS NOT NULL
       AND NOT EXISTS (
           SELECT 1 FROM pg_constraint
           WHERE conname = 'fk_cita_sede' AND conrelid = 'public.cita'::regclass
       ) THEN
        ALTER TABLE public.cita
            ADD CONSTRAINT fk_cita_sede FOREIGN KEY (id_sede)
            REFERENCES public.sede (idsede) ON DELETE RESTRICT;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ix_cita_sede ON public.cita (id_sede);
