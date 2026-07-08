# Modelo de precios y cuotas — cómo cobra el profe

> Documento de diseño del dominio de facturación. Capturado de la realidad
> de los clubes donde trabaja Lucas (08/07/2026). Define el ORDEN de
> construcción: Grupos → Turnos → Cuotas. Ver ADR-0008.

---

## 1. Cómo cobra un profesor (la realidad)

- El profe define un **valor hora** de clase.
- Si la clase es **grupal**, el valor hora se **divide entre los presentes
  del turno**: grupo de 4 → cada uno paga ¼ del valor hora.
- Cada alumno tiene una **modalidad de pago**:
  - **Por clase**: paga cada clase que toma.
  - **Mensual**: paga el mes completo (sus clases del mes).
- Lo que un alumno debe **no es un monto fijo**: depende de cuántas clases
  tomó y de cuántos eran en cada una.

## 2. La fórmula

```
Deuda de un alumno en el período =
    Σ  (valorHora ÷ cantidadDeASIGNADOSalTurno)
    por cada turno del período al que estuvo asignado
```

**El divisor son los ASIGNADOS, no los presentes** (corrección de Lucas,
08/07/2026): en un grupo de 4, si falta uno, todos pagan ¼ igual — incluido
el ausente. La cuota se debe del 1 al 10 pase lo que pase.

- **La asistencia NO mueve la plata**: es registro + input para que el
  profe decida recuperaciones. El cargo nace del roster del turno.
- **Recuperación**: decisión discrecional del profe (si el alumno canceló,
  puede reasignarle otro turno). No es automática.

**Consecuencia estructural**: la cuota es HIJA del turno. Sin saber quién
estaba asignado a qué turno y cuántos eran, no hay cálculo automático
posible. Por eso el orden de construcción es:

1. **Grupos** (grupos fijos — el modelo ya existe en la base)
2. **Horarios/Turnos** (la agenda: quién juega, cuándo, cuántos)
3. **Cuotas** (nace calculando sola desde los turnos)

## 3. Decisiones ya tomadas para Cuotas (siguen válidas)

| Tema | Decisión |
|---|---|
| Vencimiento | Día 10 del mes: 1-10 Pendiente, 11+ sin pagar → **Vencida**. El estado se CALCULA (no se guarda — misma lección que `esMenor`) |
| Confirmar pago | Registra **fecha** (server) + **medio**: Efectivo / Transferencia / Otro. Pago total, sin parciales (se evalúa después) |
| Monto | **Snapshot** al generarse el cargo (si el valor hora cambia, los cargos ya generados no se tocan) |

## 4. Pendientes a definir cuando llegue la vertical Cuotas

- [ ] ¿El valor hora es único por profe o varía por tipo de clase/categoría?
- [ ] ¿Descuento por hermanos u otros ajustes manuales?
- [ ] Pagos parciales (hoy: no)
- [ ] **Recuperación y el divisor del turno destino**: si Juan recupera
      metiéndose en un grupo de 3, ¿los otros pasan a pagar ¼? Hipótesis:
      NO — la recuperación no genera cargo ni cambia el divisor del turno
      destino (Juan ya pagó su clase original). Confirmar con la realidad.

~~Alumno que falta: ¿paga igual?~~ → **RESUELTO (08/07/2026)**: paga igual,
siempre. El divisor son los asignados, no los presentes (ver §2).
