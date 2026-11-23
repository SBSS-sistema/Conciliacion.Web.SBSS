-- Additive relationship from an appointment to the existing person and matter catalogs.
-- Existing appointments remain valid; new forms require both selections in the application.
ALTER TABLE public.cita
    ADD COLUMN IF NOT EXISTS id_persona integer,
    ADD COLUMN IF NOT EXISTS id_materia integer;

DO $$
BEGIN
    IF to_regclass('public.persona') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_cita_persona') THEN
        ALTER TABLE public.cita
            ADD CONSTRAINT fk_cita_persona FOREIGN KEY (id_persona)
            REFERENCES public.persona (idpersona) ON DELETE RESTRICT;
    END IF;

    IF to_regclass('public.materia') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_cita_materia') THEN
        ALTER TABLE public.cita
            ADD CONSTRAINT fk_cita_materia FOREIGN KEY (id_materia)
            REFERENCES public.materia (idmateria) ON DELETE RESTRICT;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ix_cita_persona ON public.cita (id_persona);
CREATE INDEX IF NOT EXISTS ix_cita_materia ON public.cita (id_materia);
