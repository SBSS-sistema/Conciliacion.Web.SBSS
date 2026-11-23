# Habilitar el código de acceso por correo

La verificación se mantiene desactivada hasta completar estos pasos. Los correos de
destino pueden ser personales (Gmail, Outlook u otro). Gmail SMTP es el remitente:
usa una cuenta Gmail dedicada para la aplicación si es posible.

1. Ejecuta `08_email_login_code_supabase.sql` en el proyecto Supabase que usa la app.
2. En la cuenta Google que enviará los códigos, activa la verificación en dos pasos
   y genera una contraseña de aplicación. No uses la contraseña normal de Gmail.
3. Añade estas variables como secretos en Render:
   - `Authentication__EmailCode__Enabled=true`
   - `Authentication__EmailCode__HashKey`: secreto aleatorio de al menos 32 caracteres.
   - `Email__Gmail__Address`: cuenta Gmail remitente completa.
   - `Email__Gmail__AppPassword`: contraseña de aplicación de Google (puedes pegarla con o sin espacios).
   - `Email__Gmail__FromName` (opcional): nombre visible del remitente.
4. Guarda los cambios, espera el reinicio de Render y prueba el flujo con un usuario
   de prueba cuyo correo esté bajo tu control.

No guardes las claves en `appsettings.json` ni las compartas en GitHub o en el chat.
Si todavía no se configuran esas variables, el inicio de sesión conserva el flujo actual.
No compartas la contraseña de aplicación ni la clave `HashKey` en GitHub o en el chat.
