-- Run after 06_roles_citas_supabase.sql and before enabling the new app version.
-- Review the accounts first. Do not promote an account by guessing its ID.
SELECT u.idusuario, u.usuario, t.emailtrabajador AS correo,
       t.estadotrabajador AS estado_trabajador, u.rol
FROM public.usuario u
JOIN public.trabajador t ON t.idtrabajador = u.idtrabajador
ORDER BY u.idusuario;

-- Normalize known role labels to the values recognized by the application.
UPDATE public.usuario
SET rol = CASE upper(trim(rol))
    WHEN 'ADMIN' THEN 'ADMINISTRADOR'
    WHEN 'ADMINISTRADOR' THEN 'ADMINISTRADOR'
    WHEN 'MESA DE PARTES' THEN 'MESA_DE_PARTES'
    WHEN 'MESA_PARTES' THEN 'MESA_DE_PARTES'
    WHEN 'MESA_DE_PARTES' THEN 'MESA_DE_PARTES'
    WHEN 'CONCILIADOR' THEN 'CONCILIADOR'
    ELSE rol
END
WHERE upper(trim(rol)) IN ('ADMIN', 'ADMINISTRADOR', 'MESA DE PARTES', 'MESA_PARTES', 'MESA_DE_PARTES', 'CONCILIADOR');

-- Example only: uncomment and replace the username with the reviewed account.
-- UPDATE public.usuario u
-- SET rol = 'ADMINISTRADOR'
-- WHERE u.usuario = 'REEMPLAZAR_CON_USUARIO_ADMIN';

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM public.usuario u
        JOIN public.trabajador t ON t.idtrabajador = u.idtrabajador
        WHERE upper(trim(u.rol)) = 'ADMINISTRADOR'
          AND upper(coalesce(t.estadotrabajador, '')) = 'ACTIVO'
    ) THEN
        RAISE EXCEPTION 'No hay un usuario activo con rol ADMINISTRADOR. Revise los usuarios y asigne el rol manualmente antes de desplegar.';
    END IF;
END $$;
