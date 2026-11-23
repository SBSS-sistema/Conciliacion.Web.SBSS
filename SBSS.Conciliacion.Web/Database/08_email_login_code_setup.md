# Habilitar el código de acceso por correo

La verificación está desactivada hasta completar estos pasos. Los trabajadores
pueden recibir los códigos en cuentas personales (Gmail, Outlook u otro). Resend
se utiliza únicamente como servicio remitente.

1. Ejecuta `08_email_login_code_supabase.sql` en Supabase si todavía no lo ejecutaste.
2. En Resend, abre **Domains** y agrega un dominio que controles, por ejemplo
   `gruposbss.com`. Copia los registros DNS que Resend indique al proveedor donde
   administras el dominio. Espera a que Resend muestre el dominio como verificado.
3. Crea una API key con permiso de envío y guarda su valor directamente en Render.
4. En Render → servicio web → **Environment**, agrega estos secretos:
   - `Authentication__EmailCode__Enabled=true`
   - `Authentication__EmailCode__HashKey`: secreto aleatorio de al menos 32 caracteres.
   - `Email__Resend__ApiKey`: API key de Resend.
   - `Email__Resend__From`: nombre y remitente del dominio verificado, por ejemplo
     `Gestión de Conciliación SBSS <no-reply@gruposbss.com>`.
5. Guarda los cambios, espera el despliegue y prueba el flujo con un usuario cuyo
   correo puedas revisar.

No uses `onboarding@resend.dev` como remitente de producción: la guía de inicio de
Resend lo usa para pruebas. No guardes la API key ni la clave `HashKey` en
`appsettings.json`, GitHub o el chat. Si todavía no se configuran las variables,
el inicio de sesión conserva el flujo actual.
