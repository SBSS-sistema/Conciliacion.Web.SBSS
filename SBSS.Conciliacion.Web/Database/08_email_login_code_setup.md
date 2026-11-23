# Habilitar el código de acceso por correo

La verificación se mantiene desactivada hasta completar estos pasos. Los correos de
destino pueden ser personales (Gmail, Outlook u otro); el remitente debe estar
autorizado por el proveedor de correo.

1. Ejecuta `08_email_login_code_supabase.sql` en el proyecto Supabase que usa la app.
2. Configura un remitente en Resend y verifica el dominio remitente en el panel del
   proveedor. La dirección `From` debe pertenecer a ese dominio.
3. Añade estas variables como secretos en Render:
   - `Authentication__EmailCode__Enabled=true`
   - `Authentication__EmailCode__HashKey`: secreto aleatorio de al menos 32 caracteres.
   - `Email__Resend__ApiKey`: API key de Resend.
   - `Email__Resend__From`: nombre y correo remitente autorizado.
4. Reinicia el servicio y prueba el flujo con un usuario de prueba cuyo correo esté
   bajo tu control.

No guardes las claves en `appsettings.json` ni las compartas en GitHub o en el chat.
Si todavía no se configuran esas variables, el inicio de sesión conserva el flujo actual.
