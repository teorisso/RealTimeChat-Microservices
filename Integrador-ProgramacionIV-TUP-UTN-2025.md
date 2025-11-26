

**Trabajo Integrador: Sistemas mensajería en tiempo real**

*Programación IV \- TUP \- UTN \- 2025*  
*Version 2.0-20251003*

[**Descripción/Especificación	2**](#descripción/especificación)

[**Escenario: Mensajería en tiempo real	2**](#escenario:-mensajería-en-tiempo-real)

[Requerimientos Funcionales	2](#requerimientos-funcionales)

[REQ-01 Gestión de Usuarios y Autenticación	2](#req-01-gestión-de-usuarios-y-autenticación)

[REQ-02 API Usuarios/Auth Service	2](#req-02-api-usuarios/auth-service)

[REQ-03 API Mensajes Service	3](#req-03-api-mensajes-service)

[REQ-04 API Grupos Service	3](#req-04-api-grupos-service)

[REQ-05 Funcionalidades de Mensajería	3](#req-05-funcionalidades-de-mensajería)

[REQ-06 Chats Grupales	3](#req-06-chats-grupales)

[REQ-07 Indicadores en Tiempo Real	3](#req-07-indicadores-en-tiempo-real)

[REQ-08 Acuses de Lectura / "Visto"	4](#req-08-acuses-de-lectura-/-"visto")

[REQ-09 Listado de usuarios lectura con fecha/hora de lectura	4](#req-09-listado-de-usuarios-lectura-con-fecha/hora-de-lectura)

[REQ-10 Autenticación y Seguridad	4](#req-10-autenticación-y-seguridad)

[REQ-11 Interfaz de Usuario (UI)	4](#req-11-interfaz-de-usuario-\(ui\))

[**Rúbrica de puntos	4**](#rúbrica-de-puntos)

Información del Trabajo Integrador  
El presente integrador siguientes pautas:

1. El siguiente trabajo práctico tiene nota grupal, basado en la rúbrica que se adjunta.  
2. Los grupos deben ser de hasta 3\~5 personas como máximo  
3. Se debe poder crear un repositorio grupal en Github proporcionado por la cátedra  
4. En el presente documento se presentan los requerimientos funcionales  
5. La rúbrica tiene una suma 100 puntos (nota 10 diez)  
6. Se requiere que el trabajo presentado cumpla con los requisitos técnicos propuestos en el apartado de igual nombre  
7. Presentación documento con resumen del trabajo presentado (Formato APA). Donde se deberá contar un resumen del proceso de investigación y armado de la presentación. Las fuentes consultadas.  
8. Presentación (PPT) para el coloquio en clase  
9. Aplicación funcionando presentada durante el coloquio

Fecha de Entregas:  

* …

Se desea que se entregue

* En Grupo en un repositorio en GitHub el código fuente  
* Informe (formato PDF)  
* Coloquio demostración con la aplicación funcionando

# Descripción/Especificación {#descripción/especificación}

En la actualidad, las aplicaciones de mensajería en tiempo real son fundamentales para la comunicación digital. Este trabajo práctico busca que desarrollen un sistema de mensajería similar a WhatsApp, Telegram o similar, implementando arquitectura de microservicios con .NET 9 (o superior), comunicación bidireccional mediante WebSockets/SignalR, y persistencia en base de datos con capacidades de tiempo real. El proyecto integra conceptos clave de APIs REST, autenticación segura con JWT, diseño de servicios desacoplados y manejo de eventos en tiempo real, competencias esenciales para el desarrollo de aplicaciones web modernas y escalables.

# Escenario: Mensajería en tiempo real {#escenario:-mensajería-en-tiempo-real}

Se seleccionó un grupo de la empresa UTN Inc. para coordinar el diseño, desarrollo e implementación de la aplicación de Mensajeria en Tiempo real

## Requerimientos Funcionales {#requerimientos-funcionales}

### REQ-01 Gestión de Usuarios y Autenticación {#req-01-gestión-de-usuarios-y-autenticación}

Registro de usuarios con validación de datos (email único, contraseña fuerte).  
Inicio de sesión que retorna access token y refresh token.  
Perfil de usuario con posibilidad de consultar y actualizar datos básicos (nombre, avatar opcional).  
Alternativamente, pueden incluirse usuarios semilla (seed data) para pruebas rápidas.

*Nota: Pueden fusionar Usuarios y Auth en un solo servicio si se justifica arquitectónicamente, pero Mensajes y Grupos deben mantenerse separados.*

### REQ-02 API Usuarios/Auth Service  {#req-02-api-usuarios/auth-service}

Módulo del servicio API para realizar Registro, Login.  
Gestión de perfil básico.Obtener Datos del usuario.  
Validación y emisión de tokens JWT

### REQ-03 API Mensajes Service {#req-03-api-mensajes-service}

Módulo del servicio API para el envío y listado de mensajes por conversación (paginado)  
Manejo de eventos "escribiendo..." mediante SignalR  
Registro de acuses de lectura/visto por mensaje y usuario  
Persistencia de mensajes con timestamps

### REQ-04 API Grupos Service {#req-04-api-grupos-service}

Modulo de API para los servicios de grupo, Creación y eliminación de grupos  
Gestión de miembros (agregar/quitar participantes)  
Listado de miembros y grupos del usuario

### REQ-05 Funcionalidades de Mensajería {#req-05-funcionalidades-de-mensajería}

Chats 1:1 (Directos).   
Iniciar conversación entre dos usuarios.  
Enviar y recibir mensajes en tiempo real.  
Visualizar historial paginado de mensajes.

### REQ-06 Chats Grupales {#req-06-chats-grupales}

Crear grupos con múltiples participantes.  
Enviar mensajes que lleguen a todos los miembros conectados.  
Agregar/remover miembros del grupo.  
Visualizar lista de participantes.

### REQ-07 Indicadores en Tiempo Real {#req-07-indicadores-en-tiempo-real}

"Está escribiendo…{UsuarioA}"  
Detectar cuando un usuario está tecleando un mensaje.  
Transmitir el evento a otros participantes del chat mediante SignalR.  
Ocultar el indicador después de \~3 segundos sin actividad o al enviar el mensaje.

### REQ-08 Acuses de Lectura / "Visto" {#req-08-acuses-de-lectura-/-"visto"}

Registrar cuándo cada usuario lee cada mensaje (timestamp).  
Persistir en base de datos la lectura del mensaje.  
Sincronizar el estado de lectura en tiempo real a todos los clientes del chat.  
Mostrar indicadores visuales en la UI (ej: doble check azul).

### REQ-09 Listado de usuarios lectura con fecha/hora de lectura {#req-09-listado-de-usuarios-lectura-con-fecha/hora-de-lectura}

Poder visualizar el listado o historial de lectura de un mensaje

### REQ-10 Autenticación y Seguridad {#req-10-autenticación-y-seguridad}

Todos los endpoints (excepto register/login) deben requerir autenticación JWT.  
SignalR Hub debe validar tokens en la conexión y rechazar clientes no autenticados.  
Implementar autorización por recursos: un usuario solo puede acceder a chats en los que participa.  
Hash de contraseñas con algoritmos seguros  
Configuración de CORS adecuada para el frontend.

### 

### REQ-11 Interfaz de Usuario (UI) {#req-11-interfaz-de-usuario-(ui)}

Desarrollar una UI mínima funcional para probar todas las funcionalidades.  
No se evalúa diseño visual: se aceptan templates, librerías de componentes o UIs básicas.  
Frameworks aceptados: React, Vue, Angular, Blazor, o HTML/JS vanilla.  
La UI debe permitir:

* Login/registro  
* Listar chats  
* Abrir chat y ver mensajes  
* Enviar mensajes  
* Ver indicadores "escribiendo" y "visto"  
* Crear grupos y agregar miembros

# Rúbrica de puntos {#rúbrica-de-puntos}

|  | … | .. | .. | . | Coloquio (Personal) |
| :---- | ----- | ----- | ----- | ----- | ----- |
| **Puntos** | **30** | **20** | **20** | **20** | **30** |

| Entrega Excelente (100%) | Se aborda el tema. Se presenta la idea y se profundiza la misma o agregando valor. La entrega se realiza en tiempo y forma. El trabajo está estructurado y completado al 100\. Se detalla en el coloquio de principio a fin el proceso completo y los problemas en el mismo. Se extendió lo que se propuso como TP. Se mejoró tareas previamente realizadas. El trabajo se presentó con todos los lineamientos propuestos en tiempo y forma. |  |  |  |  |  |  |
| :---- | :---- | ----- | ----- | ----- | ----- | ----- | ----- |
| Terminado Satisfactorio (80%) | Se aborda el tema, pero se encuentra en un 75% el punto abordado. La entrega se realiza pero existen puntos faltantes para completar la idea de la funcionalidad requerida. Se entrega en tiempo y forma. No se extendió en lo que se propuso como ideas en el TP. El trabajo o item falto algunos puntos a tener en cuenta para completarlo. |  |  |  |  |  |  |
| Basico (60%) | Se aborda el tema pero con un nivel escaso de comprensión y de realización. Se encuentra realizado al 50% del ítem solicitado. No se extendió en mejorar o perfeccionar. Se encuentra deficiente la organización del trabajo. No se detalla en el coloquio parte del ítem o se argumenta. No se presentan todos los lineamientos propuestos para el ítem. |  |  |  |  |  |  |
| No realizado/Escaso (0%) | Solo se menciona el tema o no se aborda. No presenta información relacionada al ítem solicitado. O se realizó pero con error en el abordaje para su funcionamiento o publicación. No se estructuró el trabajo. La entrega no se realizó en tiempo y forma |  |  |  |  |  |  |

