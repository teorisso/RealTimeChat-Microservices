import React, { useState } from 'react';
import { useAuthStore } from '../../store/authStore';
import type { MessageDto } from '../../types';
import clsx from 'clsx';
import { format } from 'date-fns';
import { Check, CheckCheck } from 'lucide-react';
import ReadReceiptsList from './ReadReceiptsList';

interface MessageBubbleProps {
    message: MessageDto;
}

const MessageBubble: React.FC<MessageBubbleProps> = ({ message }) => {
    const { user } = useAuthStore();
    const isOwnMessage = message.remitenteId === user?.id;
    const [showReceipts, setShowReceipts] = useState(false);

    const handleReceiptClick = (e: React.MouseEvent) => {
        e.stopPropagation();
        if (isOwnMessage) {
            setShowReceipts(true);
        }
    };

    return (
        <>
            <div className={clsx("flex w-full mt-2 space-x-3 max-w-xs", isOwnMessage ? "ml-auto justify-end" : "")}>
                {!isOwnMessage && (
                    <div className="flex-shrink-0 h-8 w-8 rounded-full bg-gray-300 flex items-center justify-center text-xs font-medium">
                        {message.remitenteNombre.charAt(0).toUpperCase()}
                    </div>
                )}
                <div>
                    <div className={clsx(
                        "p-3 rounded-lg text-sm shadow-sm relative group",
                        isOwnMessage ? "bg-blue-600 text-white rounded-br-none" : "bg-white text-gray-900 rounded-bl-none"
                    )}>
                        {!isOwnMessage && (
                            <p className="text-xs font-bold text-blue-600 mb-1">
                                {message.remitenteNombre}
                            </p>
                        )}
                        <p>{message.contenido}</p>
                        <div className={clsx("text-[10px] mt-1 flex items-center justify-end space-x-1", isOwnMessage ? "text-blue-100" : "text-gray-400")}>
                            <span>{format(new Date(message.fechaEnvio), 'h:mm a')}</span>
                            {isOwnMessage && (
                                <span onClick={handleReceiptClick} className="cursor-pointer hover:opacity-80">
                                    {message.cantidadLecturas > 0 ? (
                                        <CheckCheck className="w-3 h-3 text-blue-200" />
                                    ) : (
                                        <Check className="w-3 h-3 text-blue-200" />
                                    )}
                                </span>
                            )}
                        </div>
                    </div>
                </div>
            </div>
            {showReceipts && (
                <ReadReceiptsList messageId={message.id} onClose={() => setShowReceipts(false)} />
            )}
        </>
    );
};

export default MessageBubble;
