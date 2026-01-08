"use client"

import { useState, useEffect } from "react"
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query"
import { Button } from "@/components/ui/button"
import { Card } from "@/components/ui/card"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { Badge } from "@/components/ui/badge"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import Link from "next/link"
import { ArrowLeft, Check, X, Trash2, UserCheck, Users, Clock, Shield } from "lucide-react"
import { adminApi } from "@/lib/api/client"
import { useUser } from "@/contexts/UserContext"
import { useRouter } from "next/navigation"
import type { User } from "@/lib/api/types"

export default function AdminUsersPage() {
  const { isAdmin, isAuthenticated, isLoading: authLoading } = useUser()
  const router = useRouter()
  const queryClient = useQueryClient()

  const [selectedUser, setSelectedUser] = useState<User | null>(null)
  const [actionType, setActionType] = useState<"approve" | "reject" | "delete" | null>(null)

  // Redirect if not admin
  useEffect(() => {
    if (!authLoading && (!isAuthenticated || !isAdmin)) {
      router.push("/")
    }
  }, [isAuthenticated, isAdmin, authLoading, router])

  // Fetch all users
  const { data: allUsers = [], isLoading: loadingAll } = useQuery({
    queryKey: ["admin", "users", "all"],
    queryFn: () => adminApi.getAllUsers(),
    enabled: isAdmin,
  })

  // Fetch pending users
  const { data: pendingUsers = [], isLoading: loadingPending } = useQuery({
    queryKey: ["admin", "users", "pending"],
    queryFn: () => adminApi.getPendingUsers(),
    enabled: isAdmin,
  })

  // Mutations
  const approveMutation = useMutation({
    mutationFn: (userId: string) => adminApi.approveUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin", "users"] })
      setSelectedUser(null)
      setActionType(null)
    },
  })

  const rejectMutation = useMutation({
    mutationFn: (userId: string) => adminApi.rejectUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin", "users"] })
      setSelectedUser(null)
      setActionType(null)
    },
  })

  const deleteMutation = useMutation({
    mutationFn: (userId: string) => adminApi.deleteUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin", "users"] })
      setSelectedUser(null)
      setActionType(null)
    },
  })

  const handleAction = (user: User, action: "approve" | "reject" | "delete") => {
    setSelectedUser(user)
    setActionType(action)
  }

  const confirmAction = () => {
    if (!selectedUser) return

    switch (actionType) {
      case "approve":
        approveMutation.mutate(selectedUser.id)
        break
      case "reject":
        rejectMutation.mutate(selectedUser.id)
        break
      case "delete":
        deleteMutation.mutate(selectedUser.id)
        break
    }
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString("cs-CZ", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    })
  }

  if (authLoading) {
    return (
      <div className="container mx-auto p-6">
        <div className="animate-pulse">Loading...</div>
      </div>
    )
  }

  if (!isAdmin) {
    return null // Will redirect
  }

  const approvedUsers = allUsers.filter(u => u.isApproved)

  return (
    <div className="container mx-auto p-6">
      {/* Header */}
      <div className="mb-6">
        <Link href="/admin" className="inline-flex items-center text-sm text-gray-600 hover:text-gray-900 mb-4">
          <ArrowLeft className="w-4 h-4 mr-2" />
          Zpět na admin
        </Link>
        <h1 className="text-3xl font-bold flex items-center gap-3">
          <Users className="w-8 h-8" />
          Správa uživatelů
        </h1>
        <p className="text-gray-600 mt-2">Schvalování a správa registrovaných uživatelů</p>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
        <Card className="p-4">
          <div className="flex items-center gap-3">
            <div className="bg-blue-100 p-2 rounded-lg">
              <Users className="w-5 h-5 text-blue-600" />
            </div>
            <div>
              <p className="text-sm text-gray-600">Celkem uživatelů</p>
              <p className="text-2xl font-bold">{allUsers.length}</p>
            </div>
          </div>
        </Card>

        <Card className="p-4">
          <div className="flex items-center gap-3">
            <div className="bg-yellow-100 p-2 rounded-lg">
              <Clock className="w-5 h-5 text-yellow-600" />
            </div>
            <div>
              <p className="text-sm text-gray-600">Čekající na schválení</p>
              <p className="text-2xl font-bold">{pendingUsers.length}</p>
            </div>
          </div>
        </Card>

        <Card className="p-4">
          <div className="flex items-center gap-3">
            <div className="bg-green-100 p-2 rounded-lg">
              <UserCheck className="w-5 h-5 text-green-600" />
            </div>
            <div>
              <p className="text-sm text-gray-600">Schválení</p>
              <p className="text-2xl font-bold">{approvedUsers.length}</p>
            </div>
          </div>
        </Card>
      </div>

      {/* Tabs */}
      <Tabs defaultValue="pending" className="space-y-4">
        <TabsList>
          <TabsTrigger value="pending" className="flex items-center gap-2">
            <Clock className="w-4 h-4" />
            Čekající ({pendingUsers.length})
          </TabsTrigger>
          <TabsTrigger value="all" className="flex items-center gap-2">
            <Users className="w-4 h-4" />
            Všichni uživatelé ({allUsers.length})
          </TabsTrigger>
        </TabsList>

        {/* Pending Users Tab */}
        <TabsContent value="pending">
          <Card>
            {loadingPending ? (
              <div className="p-8 text-center text-gray-500">Načítání...</div>
            ) : pendingUsers.length === 0 ? (
              <div className="p-8 text-center text-gray-500">
                <UserCheck className="w-12 h-12 mx-auto mb-3 text-gray-300" />
                <p>Žádní uživatelé nečekají na schválení</p>
              </div>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Email</TableHead>
                    <TableHead>Jméno</TableHead>
                    <TableHead>Registrace</TableHead>
                    <TableHead className="text-right">Akce</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {pendingUsers.map((user) => (
                    <TableRow key={user.id}>
                      <TableCell className="font-medium">{user.email}</TableCell>
                      <TableCell>{user.displayName || "-"}</TableCell>
                      <TableCell>{formatDate(user.createdAt)}</TableCell>
                      <TableCell className="text-right">
                        <div className="flex justify-end gap-2">
                          <Button
                            size="sm"
                            variant="outline"
                            className="text-green-600 border-green-300 hover:bg-green-50"
                            onClick={() => handleAction(user, "approve")}
                          >
                            <Check className="w-4 h-4 mr-1" />
                            Schválit
                          </Button>
                          <Button
                            size="sm"
                            variant="outline"
                            className="text-red-600 border-red-300 hover:bg-red-50"
                            onClick={() => handleAction(user, "reject")}
                          >
                            <X className="w-4 h-4 mr-1" />
                            Odmítnout
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </Card>
        </TabsContent>

        {/* All Users Tab */}
        <TabsContent value="all">
          <Card>
            {loadingAll ? (
              <div className="p-8 text-center text-gray-500">Načítání...</div>
            ) : allUsers.length === 0 ? (
              <div className="p-8 text-center text-gray-500">
                <Users className="w-12 h-12 mx-auto mb-3 text-gray-300" />
                <p>Žádní uživatelé</p>
              </div>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Email</TableHead>
                    <TableHead>Jméno</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Registrace</TableHead>
                    <TableHead className="text-right">Akce</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {allUsers.map((user) => (
                    <TableRow key={user.id}>
                      <TableCell className="font-medium">
                        <div className="flex items-center gap-2">
                          {user.email}
                          {user.isAdmin && (
                            <Badge variant="secondary" className="bg-purple-100 text-purple-700">
                              <Shield className="w-3 h-3 mr-1" />
                              Admin
                            </Badge>
                          )}
                        </div>
                      </TableCell>
                      <TableCell>{user.displayName || "-"}</TableCell>
                      <TableCell>
                        {user.isApproved ? (
                          <Badge className="bg-green-100 text-green-700">
                            <Check className="w-3 h-3 mr-1" />
                            Schváleno
                          </Badge>
                        ) : (
                          <Badge variant="secondary" className="bg-yellow-100 text-yellow-700">
                            <Clock className="w-3 h-3 mr-1" />
                            Čeká
                          </Badge>
                        )}
                      </TableCell>
                      <TableCell>{formatDate(user.createdAt)}</TableCell>
                      <TableCell className="text-right">
                        <div className="flex justify-end gap-2">
                          {!user.isApproved && (
                            <Button
                              size="sm"
                              variant="outline"
                              className="text-green-600 border-green-300 hover:bg-green-50"
                              onClick={() => handleAction(user, "approve")}
                            >
                              <Check className="w-4 h-4" />
                            </Button>
                          )}
                          {!user.isAdmin && (
                            <Button
                              size="sm"
                              variant="outline"
                              className="text-red-600 border-red-300 hover:bg-red-50"
                              onClick={() => handleAction(user, "delete")}
                            >
                              <Trash2 className="w-4 h-4" />
                            </Button>
                          )}
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </Card>
        </TabsContent>
      </Tabs>

      {/* Confirmation Dialog */}
      <AlertDialog open={!!selectedUser && !!actionType} onOpenChange={() => { setSelectedUser(null); setActionType(null); }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {actionType === "approve" && "Schválit uživatele?"}
              {actionType === "reject" && "Odmítnout uživatele?"}
              {actionType === "delete" && "Smazat uživatele?"}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {actionType === "approve" && (
                <>Uživatel <strong>{selectedUser?.email}</strong> bude schválen a bude moci používat systém.</>
              )}
              {actionType === "reject" && (
                <>Uživatel <strong>{selectedUser?.email}</strong> bude odmítnut a jeho účet bude smazán.</>
              )}
              {actionType === "delete" && (
                <>Uživatel <strong>{selectedUser?.email}</strong> bude trvale smazán. Tato akce je nevratná.</>
              )}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Zrušit</AlertDialogCancel>
            <AlertDialogAction
              onClick={confirmAction}
              className={
                actionType === "approve"
                  ? "bg-green-600 hover:bg-green-700"
                  : "bg-red-600 hover:bg-red-700"
              }
            >
              {actionType === "approve" && "Schválit"}
              {actionType === "reject" && "Odmítnout"}
              {actionType === "delete" && "Smazat"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
