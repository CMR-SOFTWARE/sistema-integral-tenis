// Espejo de RankingDtos.cs — el ranking es CROSS-TENANT (jugadores de todas las
// academias), a diferencia de casi todo el resto de la app.

export interface RankingFila {
  jugadorId: string;
  usuarioId: string;
  nombre: string;
  apellido: string;
  posicion: number;
  puntos: number;
  rango: string;
  cf: number;
}

export interface MiPerfilRanking {
  inscripto: boolean;
  jugadorId: string | null;
  posicion: number | null;
  puntos: number;
  rango: string | null;
  cf: number | null;
  ciudadResidencia: string | null;
  provincia: string | null;
  pais: string | null;
  bio: string | null;
  mejorPuestoHistorico: number | null;
}

export interface PerfilPublicoRanking {
  jugadorId: string;
  nombre: string;
  apellido: string;
  posicion: number | null;
  puntos: number;
  rango: string | null;
  cf: number | null;
  ciudadResidencia: string | null;
  provincia: string | null;
  bio: string | null;
  mejorPuestoHistorico: number | null;
}

export interface InscribirmeRanking {
  sexo?: string;
  ciudadResidencia?: string;
  provincia?: string;
  pais?: string;
  bio?: string;
}

export type EstadoDesafio = 'Propuesto' | 'Aceptado' | 'Finalizado';

export interface Desafio {
  id: string;
  jugador1Id: string;
  jugador1Nombre: string;
  jugador2Id: string;
  jugador2Nombre: string;
  estado: EstadoDesafio;
  creadoEl: string;
  aceptadoEl: string | null;
  ganadorId: string | null;
  puntosGanador: number | null;
  puntosPerdedor: number | null;
  finalizadoEn: string | null;
}

// ── Dobles: espejo de singles, pool de puntos independiente. JugadorId sigue
// siendo el id de SINGLES (con eso se arma un desafío) — JugadorRankingDoblesId
// es el id de la inscripción de dobles, interno, no hace falta en el front. ──

export interface RankingFilaDobles {
  jugadorRankingDoblesId: string;
  jugadorId: string;
  usuarioId: string;
  nombre: string;
  apellido: string;
  posicion: number;
  puntos: number;
  rango: string;
  cf: number;
}

export interface MiPerfilDobles {
  inscripto: boolean;
  jugadorRankingDoblesId: string | null;
  posicion: number | null;
  puntos: number;
  rango: string | null;
  cf: number | null;
  mejorPuestoHistorico: number | null;
}

export interface PerfilPublicoRankingDobles {
  jugadorId: string;
  nombre: string;
  apellido: string;
  posicion: number | null;
  puntos: number;
  rango: string | null;
  cf: number | null;
  mejorPuestoHistorico: number | null;
}

export interface DesafioDobles {
  id: string;
  jugador1Id: string;
  jugador1Nombre: string;
  jugador2Id: string;
  jugador2Nombre: string;
  rival1Id: string;
  rival1Nombre: string;
  rival2Id: string;
  rival2Nombre: string;
  estado: EstadoDesafio;
  creadoEl: string;
  aceptadoEl: string | null;
  ganoParejaA: boolean | null;
  puntosGanadores: number | null;
  puntosPerdedores: number | null;
  finalizadoEn: string | null;
}
