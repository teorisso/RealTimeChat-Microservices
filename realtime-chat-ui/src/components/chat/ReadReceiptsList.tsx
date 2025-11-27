import React, { useEffect, useState } from 'react';
import MessageService from '../../services/messageService';
import { Loader2, X, CheckCheck } from 'lucide-react';
import type { ReadReceiptDto } from '../../types';
import { format } from 'date-fns';

interface ReadReceiptsListProps {
    messageId: string;
    onClose: () => void;
}

const ReadReceiptsList: React.FC<ReadReceiptsListProps> = ({ messageId, onClose }) => {
    const [receipts, setReceipts] = useState<ReadReceiptDto[]>([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const fetchReceipts = async () => {
            try {
                const data = await MessageService.getMessageReceipts(messageId);
                setReceipts(data);
            } catch (error) {
                console.error('Failed to fetch receipts', error);
            } finally {
                setLoading(false);
            }
        };
        fetchReceipts();
    }, [messageId]);

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50 p-4" onClick={onClose}>
            <div className="bg-white rounded-lg shadow-xl w-full max-w-sm flex flex-col max-h-[60vh]" onClick={e => e.stopPropagation()}>
                <div className="flex justify-between items-center p-4 border-b">
                    <h3 className="text-lg font-semibold text-gray-900 flex items-center">
                        <CheckCheck className="w-5 h-5 text-blue-500 mr-2" />
                        Read by
                    </h3>
                    <button onClick={onClose} className="text-gray-400 hover:text-gray-500">
                        <X className="w-5 h-5" />
                    </button>
                </div>

                <div className="flex-1 overflow-y-auto p-2">
                    {loading ? (
                        <div className="flex justify-center p-4"><Loader2 className="w-6 h-6 animate-spin text-blue-500" /></div>
                    ) : receipts.length === 0 ? (
                        <div className="text-center p-4 text-gray-500 text-sm">No read receipts yet</div>
                    ) : (
                        <div className="space-y-1">
                            {receipts.map(receipt => (
                                <div key={receipt.id} className="flex items-center justify-between p-3 hover:bg-gray-50 rounded-md">
                                    <div className="flex items-center space-x-3">
                                        <div className="h-8 w-8 rounded-full bg-gray-200 flex items-center justify-center text-gray-600 font-medium text-xs">
                                            {receipt.usuarioNombre.charAt(0).toUpperCase()}
                                        </div>
                                        <div>
                                            <p className="text-sm font-medium text-gray-900">{receipt.usuarioNombre}</p>
                                            <p className="text-xs text-gray-500">
                                                {format(new Date(receipt.fechaLectura), 'MMM d, h:mm a')}
                                            </p>
                                        </div>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
};

export default ReadReceiptsList;
