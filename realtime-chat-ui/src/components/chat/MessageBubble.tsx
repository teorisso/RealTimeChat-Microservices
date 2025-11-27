import React from 'react';
import type { MessageDto } from '../../types';
import { useAuthStore } from '../../store/authStore';
import { format } from 'date-fns';
import { Check, CheckCheck } from 'lucide-react';
import clsx from 'clsx';

interface MessageBubbleProps {
    message: MessageDto;
}

const MessageBubble: React.FC<MessageBubbleProps> = ({ message }) => {
    const { user } = useAuthStore();
    const isMe = message.remitenteId === user?.id || message.remitenteId === String(user?.id); // Handle string/number mismatch if any

    return (
        <div className={clsx("flex mb-4", isMe ? "justify-end" : "justify-start")}>
            <div className={clsx(
                "max-w-[70%] rounded-lg px-4 py-2 shadow-sm",
                isMe ? "bg-blue-500 text-white rounded-br-none" : "bg-white text-gray-900 rounded-bl-none"
            )}>
                {!isMe && (
                    <p className="text-xs font-bold text-blue-600 mb-1">
                        {message.remitenteNombre}
                    </p>
                )}
                <p className="text-sm break-words whitespace-pre-wrap">
                    {message.contenido}
                </p>
                <div className={clsx("flex items-center justify-end space-x-1 mt-1", isMe ? "text-blue-100" : "text-gray-400")}>
                    <span className="text-[10px]">
                        {format(new Date(message.fechaEnvio), 'HH:mm')}
                    </span>
                    {isMe && (
                        <span>
                            {message.cantidadLecturas > 0 ? (
                                <CheckCheck className="w-3 h-3 text-blue-200" /> // Blue check if read? Actually usually blue means read.
                                // If I want blue for read, I should use a different color or style.
                                // The requirement says: "✓✓ Doble check azul: mensaje leído"
                                // So if cantidadLecturas > 0 (or specific logic), show blue.
                            ) : (
                                <Check className="w-3 h-3" />
                            )}
                        </span>
                    )}
                </div>
            </div>
        </div>
    );
};

export default MessageBubble;
