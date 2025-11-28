export interface ApiResponse<T> {
    success: boolean;
    message: string;
    data?: T;
    errors?: string[];
}

export interface UsuarioDto {
    id: string;
    nombre: string;
    email: string;
}

export interface AuthResponse {
    success: boolean;
    message: string;
    data?: {
        accessToken: string;
        refreshToken: string;
        userInfo: UsuarioDto;
    };
}

export interface ConversationDto {
    id: string;
    tipo: "directa" | "grupo";
    usuario1Id?: number;
    usuario2Id?: number;
    grupoId?: number;
    grupoNombre?: string; // NUEVO: Nombre del grupo
    fechaCreacion: string;
    ultimoMensaje?: MessageDto;
    mensajesNoLeidos: number;
    participantesIds: number[];
}

export interface MessageDto {
    id: string;
    conversacionId: string;
    remitenteId: string;
    remitenteNombre: string;
    contenido: string;
    fechaEnvio: string;
    eliminado: boolean;
    cantidadLecturas: number;
    leidoPorMi: boolean;
}

export interface GrupoDto {
    id: string;
    nombre: string;
    descripcion?: string;
    avatarUrl?: string;
    creadorId: string;
    fechaCreacion: string;
    cantidadMiembros: number;
    miembros: UsuarioDto[];
}

export interface TypingIndicatorDto {
    conversacionId: string;
    usuarioId: string;
    usuarioNombre: string;
    isTyping: boolean;
}

export interface ReadReceiptDto {
    id: string;
    mensajeId: string;
    usuarioId: string;
    usuarioNombre: string;
    fechaLectura: string;
}
