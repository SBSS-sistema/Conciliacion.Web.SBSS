-- Additive migration for email second-step login verification.
-- The app hashes each code before storing it; this table never stores the code itself.

CREATE TABLE IF NOT EXISTS public.login_email_code (
    desafio_id uuid PRIMARY KEY,
    idusuario integer NOT NULL REFERENCES public.usuario (idusuario) ON DELETE CASCADE,
    codigo_hash bytea NOT NULL,
    creado_en timestamp with time zone NOT NULL DEFAULT now(),
    expira_en timestamp with time zone NOT NULL,
    consumido_en timestamp with time zone,
    intentos smallint NOT NULL DEFAULT 0 CHECK (intentos >= 0 AND intentos <= 5)
);

CREATE INDEX IF NOT EXISTS ix_login_email_code_user_created
    ON public.login_email_code (idusuario, creado_en DESC);

ALTER TABLE public.login_email_code ENABLE ROW LEVEL SECURITY;
REVOKE ALL ON TABLE public.login_email_code FROM PUBLIC, anon, authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.login_email_code TO service_role;
